using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
[CreateAssetMenu(fileName = "NewVideoClip", menuName = "Video Clip")]
public class VideoClipData : ScriptableObject
{
    public bool looping;
    public VideoClipData nextClipID;
    public ButtonLayouts buttons;
    public List<string> onClipStartEvents;
    public List<string> onClipEndEvents;
    public List<SubtitleLine> subtitles;
}

[Serializable]
public class SubtitleLine
{
    [TextArea(1, 2)]
    public string text;
    public float startDelay;
    public float duration;
}