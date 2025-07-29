using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class VideoImportEditor : EditorWindow
{
    private string videoPath;
    private string[] foundVideos;
    private bool showVideoList;
    private Vector2 videolListScrollPos;
    private GUIStyle videoListStyle;
    
    [MenuItem("Window/Import Videos")]
    public static void ShowWindow()
    {
        GetWindow<VideoImportEditor>("Import Videos");
    }
    
    void OnGUI()
    {
        EditorGUILayout.Space(10);
        videoListStyle = new GUIStyle(EditorStyles.label);
        if (GUILayout.Button("Select Video Directory"))
        {
            videoPath = EditorUtility.OpenFolderPanel("Select Video Directory", videoPath, "");
            foundVideos = Directory.Exists(videoPath) ? Directory.GetFiles(videoPath, "*.mp4", SearchOption.AllDirectories) : null;
        }

        EditorGUILayout.Space(10);
        
        if (Directory.Exists(videoPath))
        {
            EditorGUILayout.LabelField("Video Path", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(videoPath);
            EditorGUILayout.Space(10);
            
            if (foundVideos is { Length: > 0 })
            {
                showVideoList = EditorGUILayout.BeginFoldoutHeaderGroup(showVideoList, "Found " + foundVideos.Length + " Videos");
                if (showVideoList)
                {
                    videolListScrollPos = EditorGUILayout.BeginScrollView(videolListScrollPos, GUILayout.Height(foundVideos.Length > 15 ? 300 : foundVideos.Length * 21));
                    EditorGUI.indentLevel++;
                    for (int i = 0; i < foundVideos.Length; i++)
                    {
                        if (i !=  foundVideos.Length - 1 && i % 2 == 1) videoListStyle.normal.background = MakeTex(1, 1, new Color(1,1,1,0.03f));
                        else videoListStyle.normal.background = null;
                        EditorGUILayout.LabelField(Path.GetFileName(foundVideos[i]), videoListStyle);
                    }
                    EditorGUI.indentLevel--;
                    EditorGUILayout.EndScrollView();
                }
                EditorGUILayout.EndFoldoutHeaderGroup();
                EditorGUILayout.Space(10);
                if (GUILayout.Button("Import Videos"))
                {
                    string baseOutputPath = "Assets/VideoAssets/";
                    if (!AssetDatabase.IsValidFolder(baseOutputPath)) AssetDatabase.CreateFolder("Assets", "VideoAssets");
                    
                    foreach (var fullPath in foundVideos)
                    {
                        var relativePath = Path.GetRelativePath(videoPath, fullPath);
                        var relativeDir = Path.GetDirectoryName(relativePath);
                        var fileName = Path.GetFileNameWithoutExtension(fullPath);
                        var outputDir = Path.Combine(baseOutputPath, relativeDir ?? "");
                        outputDir = outputDir.Replace('\\', '/');
                        
                        var folders = outputDir.Substring("Assets/".Length).Split('/');
                        var current = "Assets";
                        foreach (var folder in folders)
                        {
                            if (!AssetDatabase.IsValidFolder($"{current}/{folder}"))
                            {
                                AssetDatabase.CreateFolder(current, folder);
                            }
                            current += "/" + folder;
                        }

                        var assetPath = $"{outputDir}/{fileName}.asset";
                        if (AssetDatabase.AssetPathExists(assetPath)) continue;
                        
                        var newClip = CreateInstance<VideoClipData>();
                        AssetDatabase.CreateAsset(newClip, assetPath);
                    }
                    
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                    
                    EditorUtility.FocusProjectWindow();
                    Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(baseOutputPath.TrimEnd('/'));
                    EditorGUIUtility.PingObject(Selection.activeObject);
                }
            }
            else
            {
                EditorGUILayout.LabelField("No videos found!", EditorStyles.boldLabel);
            }
        }
        else
        {
            EditorGUILayout.LabelField("No video path selected!", EditorStyles.boldLabel);
        }
    }
    
    private Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; i++)
            pix[i] = col;

        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }
}

