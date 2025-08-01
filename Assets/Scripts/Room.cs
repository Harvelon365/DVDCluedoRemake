using System;
using UnityEngine;

[CreateAssetMenu(fileName = "NewRoom", menuName = "Room")]
public class Room : ScriptableObject
{
    public RoomObservation[] observations;
    public VideoClipData successClip;
}

[Serializable]
public class RoomObservation
{
    public VideoClipData observationClip;
    public RoomQuestion[] questions;
}

[Serializable]
public class RoomQuestion
{
    public VideoClipData questionClip;
    public VideoClipData answerClip;
}
