using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
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

    void OnGUI()
    {
        GUILayout.Label("Generate Subtitles from Remote Video", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Selected Folder:", GUILayout.Width(100));
        folderPath = EditorGUILayout.TextField(folderPath);

        if (GUILayout.Button("Browse", GUILayout.Width(60)))
        {
            string selected = EditorUtility.OpenFolderPanel("Select Folder with VideoClipData", "Assets", "");
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
        string[] guids = AssetDatabase.FindAssets("t:VideoClipData", new[] { folder });

        if (guids.Length == 0)
        {
            Debug.LogWarning($"No VideoClipData assets found in folder: {folder}");
            return;
        }

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            VideoClipData clipData = AssetDatabase.LoadAssetAtPath<VideoClipData>(path);
            if (clipData != null)
            {
                GenerateSubtitles(clipData);
            }
        }

        Debug.Log($"Subtitle generation complete for {guids.Length} assets.");
    }

    async void GenerateSubtitles(VideoClipData clipData)
    {
        string clipName = clipData.name;
        string url = $"https://harveytucker.com/DVDCluedo/{clipName}.mp4";
        string tempPath = Path.Combine(Application.temporaryCachePath, $"{clipName}.mp4");

        Debug.Log($"Subtitling {clipName}");

        using (WebClient client = new WebClient())
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

        if (result != null && result.Count > 0)
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
        string audioPath = Path.ChangeExtension(videoPath, ".wav");

        // Step 1: Extract audio with FFmpeg
        ProcessStartInfo ffmpegInfo = new ProcessStartInfo
        {
            FileName = "ffmpeg",
            Arguments = $"-i \"{videoPath}\" -ar 16000 -ac 1 -f wav \"{audioPath}\" -y",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using (Process ffmpeg = Process.Start(ffmpegInfo))
        {
            ffmpeg.WaitForExit();

            string error = ffmpeg.StandardError.ReadToEnd();
            if (!File.Exists(audioPath))
            {
                UnityEngine.Debug.LogError($"FFmpeg failed: {error}");
                return null;
            }
        }

        // Step 2: Call Python Whisper script
        string scriptPath = Path.Combine(Application.dataPath, "../whisper_transcribe.py");

        ProcessStartInfo whisperInfo = new ProcessStartInfo
        {
            FileName = "python",
            Arguments = $"\"{scriptPath}\" \"{audioPath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8
        };

        string jsonOutput = "";
        string pythonError = "";

        using (Process whisper = Process.Start(whisperInfo))
        {
            jsonOutput = whisper.StandardOutput.ReadToEnd();
            pythonError = whisper.StandardError.ReadToEnd();
            whisper.WaitForExit();
        }

        if (!string.IsNullOrEmpty(pythonError))
        {
            UnityEngine.Debug.LogWarning("Whisper error: " + pythonError);
        }

        try
        {
            var result = JsonConvert.DeserializeObject<List<SubtitleLine>>(jsonOutput);
            return result;
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogError($"Failed to parse subtitle JSON: {ex.Message}\nOutput was:\n{jsonOutput}");
            return null;
        }
    }
}
