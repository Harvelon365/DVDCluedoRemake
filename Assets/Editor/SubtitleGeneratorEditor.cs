using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class SubtitleGeneratorEditor : EditorWindow
{
    private string folderPath = "Assets";

    [MenuItem("Tools/Subtitle Generator")]
    public static void ShowWindow()
    {
        GetWindow<SubtitleGeneratorEditor>("Subtitle Generator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Generate Subtitles from Remote Video", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Selected Folder:", GUILayout.Width(100));
        folderPath = EditorGUILayout.TextField(folderPath);

        if (GUILayout.Button("Browse", GUILayout.Width(60)))
        {
            var selected = EditorUtility.OpenFolderPanel("Select Folder with VideoClipData", "Assets", "");
            if (!string.IsNullOrEmpty(selected))
            {
                // Convert absolute path to relative project path
                if (selected.StartsWith(Application.dataPath))
                {
                    folderPath = "Assets" + selected.Substring(Application.dataPath.Length);
                }
                else
                {
                    Debug.LogWarning("Selected folder must be within the Assets directory.");
                }
            }
        }
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Generate Subtitles for All"))
        {
            GenerateSubtitlesForAllInFolder(folderPath);
        }
    }
    
    private void GenerateSubtitlesForAllInFolder(string folder)
    {
        var guids = AssetDatabase.FindAssets("t:VideoClipData", new[] { folder });

        if (guids.Length == 0)
        {
            Debug.LogWarning($"No VideoClipData assets found in folder: {folder}");
            return;
        }

        foreach (string guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var clipData = AssetDatabase.LoadAssetAtPath<VideoClipData>(path);
            if (clipData != null)
            {
                GenerateSubtitles(clipData);
            }
        }

        Debug.Log($"Subtitle generation complete for {guids.Length} assets.");
    }

    private async void GenerateSubtitles(VideoClipData clipData)
    {
        var clipName = clipData.name;
        var url = $"https://harveytucker.com/DVDCluedo/{clipName}.mp4";
        var tempPath = Path.Combine(Application.temporaryCachePath, $"{clipName}.mp4");

        Debug.Log($"Subtitling {clipName}");

        using (var client = new WebClient())
        {
            try
            {
                await client.DownloadFileTaskAsync(url, tempPath);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error downloading file: {e.Message}");
                return;
            }
        }

        // Transcribe and populate subtitles
        var result = TranscribeVideoToSubtitles(tempPath);

        if (result is { Count: > 0 })
        {
            clipData.subtitles = result;
            EditorUtility.SetDirty(clipData);
            AssetDatabase.SaveAssets();
            //Debug.Log($"Generated {result.Count} subtitles.");
        }
        else
        {
            Debug.LogWarning("No subtitles generated.");
        }
    }

    private List<SubtitleLine> TranscribeVideoToSubtitles(string videoPath)
    {
        var audioPath = Path.ChangeExtension(videoPath, ".wav");

        // Step 1: Extract audio with FFmpeg
        var ffmpegInfo = new ProcessStartInfo
        {
            FileName = "ffmpeg",
            Arguments = $"-i \"{videoPath}\" -ar 16000 -ac 1 -f wav \"{audioPath}\" -y",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using (var ffmpeg = Process.Start(ffmpegInfo))
        {
            if (ffmpeg != null)
            {
                ffmpeg.WaitForExit();

                var error = ffmpeg.StandardError.ReadToEnd();
                if (!File.Exists(audioPath))
                {
                    Debug.LogError($"FFmpeg failed: {error}");
                    return null;
                }
            }
        }

        // Step 2: Call Python Whisper script
        var scriptPath = Path.Combine(Application.dataPath, "../whisper_transcribe.py");

        var whisperInfo = new ProcessStartInfo
        {
            FileName = "python",
            Arguments = $"\"{scriptPath}\" \"{audioPath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8
        };

        var jsonOutput = "";
        var pythonError = "";

        using (var whisper = Process.Start(whisperInfo))
        {
            if (whisper != null)
            {
                jsonOutput = whisper.StandardOutput.ReadToEnd();
                pythonError = whisper.StandardError.ReadToEnd();
                whisper.WaitForExit();
            }
        }

        if (!string.IsNullOrEmpty(pythonError))
        {
            Debug.LogWarning("Whisper error: " + pythonError);
        }

        try
        {
            var result = JsonConvert.DeserializeObject<List<SubtitleLine>>(jsonOutput);
            return result;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to parse subtitle JSON: {ex.Message}\nOutput was:\n{jsonOutput}");
            return null;
        }
    }
}
