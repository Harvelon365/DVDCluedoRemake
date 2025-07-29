import sys
import whisper
import json
import os
import re

def create_segments(words):
    segments = []
    current_segment = []

    for word in words:
        current_segment.append(word)
        if word["word"].endswith(('.', ',', '!', '?')) and len(current_segment) > 2:
            segments.append(current_segment)
            current_segment = []

    return segments

def create_sentences(segments):
    sentences = []

    for segment in segments:
        text = ""
        start = 1000000.0
        end = 0.0
        split_line = False
        for word in segment:
            if len(text) > 42 and not split_line:
                text += "\n"
                word["word"] = word["word"][1:]
                split_line = True
            text += word["word"]
            if word["start"] < start:
                start = word["start"]
            if word["end"] > end:
                end = word["end"]

        if text.startswith(' '):
            text = text[1:]
        sentences.append({"text": text, "start": start, "end": end})

    return sentences

def generate_subtitles(wav_path):
    model = whisper.load_model("small")
    result = model.transcribe(wav_path, word_timestamps=True)

    words = []
    subtitles = []

    for seg in result["segments"]:
        for word in seg["words"]:
            words.append(word)

    segments = create_segments(words)
    sentences = create_sentences(segments)

    prev_end = 0.0

    for s in sentences:
        start_delay = round(s["start"] - prev_end, 2)
        duration = round(s["end"] - s["start"], 2)
        subtitles.append({
            "text": s["text"],
            "startDelay": max(start_delay, 0),
            "duration": duration,
        })
        prev_end = s["end"]

    return subtitles


if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("Usage: python generate_subtitles.py path/to/audio.wav")
        sys.exit(1)

    wav_path = sys.argv[1]
    if not os.path.isfile(wav_path):
        print(f"File not found: {wav_path}")
        sys.exit(1)

    subtitles = generate_subtitles(wav_path)
    print(json.dumps(subtitles, ensure_ascii=False, indent=2))
