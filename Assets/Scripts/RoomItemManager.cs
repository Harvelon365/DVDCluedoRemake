using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class RoomItemManager : MonoBehaviour
{
    public static RoomItemManager Instance;
    
    private Dictionary<Room, HashSet<RoomQuestion>> usedQuestions = new Dictionary<Room, HashSet<RoomQuestion>>();

    private void Awake()
    {
        Instance = this;
    }

    public void GetRandomQuestionFromRoom(Room room, out RoomObservation selectedObservation, out RoomQuestion selectedQuestion)
    {
        while (true)
        {
            selectedObservation = null;
            selectedQuestion = null;

            if (!usedQuestions.ContainsKey(room)) usedQuestions[room] = new HashSet<RoomQuestion>();

            var usedQs = usedQuestions[room];
            var unusedQs = new List<(RoomObservation, RoomQuestion)>();

            foreach (var observation in room.observations)
            {
                foreach (var question in observation.questions)
                {
                    if (!usedQs.Contains(question)) unusedQs.Add((observation, question));
                }
            }

            if (unusedQs.Count == 0)
            {
                ResetUsedQuestions(room);
                continue;
            }

            var randomSelection = unusedQs[Random.Range(0, unusedQs.Count)];
            selectedObservation = randomSelection.Item1;
            selectedQuestion = randomSelection.Item2;

            usedQs.Add(selectedQuestion);
            break;
        }
    }

    private void ResetUsedQuestions(Room room)
    {
        if (usedQuestions.ContainsKey(room)) usedQuestions[room].Clear();
    }

    public void ResetAll()
    {
        usedQuestions.Clear();
    }
}