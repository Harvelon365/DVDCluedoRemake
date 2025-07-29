using System;
using UnityEngine;

[CreateAssetMenu(fileName = "NewCase", menuName = "Case")]
public class Case : ScriptableObject
{
    public VideoClipData[] setupClips;
    public VideoClipData players3Clip;
    public VideoClipData players4Clip;
    public VideoClipData players5Clip;
    public VideoClipData introClip;
    public VideoClipData menuClip;
    public VideoClipData endingClip;
    public bool startSecretPassage = false;
    public bool startSummonButler = false;
    public bool startItemCard = false;
    public bool startInspectorNote = false;
    public VideoClipData[] eventClips;
	public VideoClipData[] secretPassageClips;
    public Sprite[] noteNumberSprites;
    public VideoClipData[] butlerClips;
    public Room[] observableRooms;
    public VideoClipData roomMenu;
}
