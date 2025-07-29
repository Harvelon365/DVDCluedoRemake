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
