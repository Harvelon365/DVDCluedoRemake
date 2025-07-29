using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    #region Inspector Variables
    
    [Header("Settings")]
    [SerializeField] private bool showIntro;
    public bool showSubtitles;
    public bool enableMenuMusic;
    [Range(1, 5)] public float gameSpeed;
    [Range(0f, 1f)] public float gameVolume;
    public bool highlightFirstButton;
    [Space(20)]
    [SerializeField] private Case[] cases;
    
    [Header("UI")] 
    public VideoPlayer videoPlayer;
    public GameObject settingsMenu;
    public Button settingsButton;
    public Button reloadClipButton;
    [SerializeField] private TMP_Text subtitleText;
    [SerializeField] private RectTransform subtitleBackground;
    [SerializeField] private Image selectionLetterImage;
    [SerializeField] private Image noteNumberImage;
    [SerializeField] private GameObject loadingScreen;
    [SerializeField] private GameObject loadButton;
    [SerializeField] private GameObject continueButton;
    private int subtitleIndex;
    [HideInInspector] public ButtonLayout currentLayout;

    [Header("Setup Clips")] [SerializeField]
    private List<VideoClipData> setupClips;
    [SerializeField] private VideoClipData players3DealClip;
    [SerializeField] private VideoClipData players4DealClip;
    [SerializeField] private VideoClipData players5DealClip;
    private int nextSetupClip;

    [Header("Game Clips")]
    [SerializeField] private VideoClipData mainMenuClip;
    [SerializeField] private VideoClipData inspectorCallClip;
    [SerializeField] private VideoClipData[] noteInstructionClips;
    [SerializeField] private VideoClipData[] noteMenuClips;
    [SerializeField] private VideoClipData noteNumberClip;
    [SerializeField] private VideoClipData selectionLetterReadyClip;
    [SerializeField] private VideoClipData passageStartClip;
    [SerializeField] private VideoClipData passageEndClip;
    [SerializeField] private VideoClipData accusationStartClip;
    [SerializeField] private VideoClipData[] resultsClips;
    
    [Header("Overflow Clips")]
    [SerializeField] private VideoClipData overflowEventClip;
    [SerializeField] private VideoClipData overflowSecretPassageClip;
    [SerializeField] private VideoClipData overflowButlerClip;
    

    [Header("Utility Clips")]
    [SerializeField] private VideoClipData restartGameClip;
    [SerializeField] private VideoClipData staticButtonsClip;
    [SerializeField] private VideoClipData disabledButtonsClip;

    [Header("Button Layouts")] 
    [SerializeField] private ButtonLayout mainMenu;
    [SerializeField] private ButtonLayout caseMenu;
    [SerializeField] private ButtonLayout passageOk;
    [SerializeField] private ButtonLayout inspectorCall;
    [SerializeField] private ButtonLayout menuRepeat;
    [SerializeField] private ButtonLayout eventOkMenu;
    [SerializeField] private ButtonLayout eventOptionMenu;
    [SerializeField] private ButtonLayout setupRepeat;
    [SerializeField] private ButtonLayout playerCount;
    [SerializeField] private ButtonLayout selectionLetterReady;
    [SerializeField] private ButtonLayout selectionLetterConfirm;
    [SerializeField] private ButtonLayout resultsOk;
    [SerializeField] private ButtonLayout noteInstructionsRepeat;
    [SerializeField] private ButtonLayout noteNumberOk;
    [SerializeField] private ButtonLayout noteSelect2;
    [SerializeField] private ButtonLayout noteSelect3;
    [SerializeField] private ButtonLayout roomSelect;
    [SerializeField] private ButtonLayout roomAnswer;
    [SerializeField] private ButtonLayout roomQuestion;
    
    #endregion

    #region Private Variables
    
    private Case currentCase;
    private List<List<int>> solutions;
    
    private VideoPlayer.EventHandler loopPointHandler;
    private int eventCountdown;
    private int nextEventIndex;
    private int correct;
    private bool isPaused;
    private bool isLoading;
    private Coroutine showLoadingScreenCoroutine;
    private Coroutine showNextSubtitleCoroutine;
    
    private VideoClipData currentClipData;
    private VideoClipData previousClip;
    
    private bool enableSecretPassage;
    private int nextPassageClip;
    
    private bool enableSummonButler;
    private int nextButlerIndex;
    
    private bool enableItemCard;
    private int availableRooms;
    private Room currentRoom;
    private RoomObservation currentObservation;
    private RoomQuestion currentQuestion;
    private VideoClipData lastRoomAddedFrom;
    
    private bool enableInspectorNote;
    private int availableNotes;
    private VideoClipData lastNoteAddedFrom;

    #endregion

    private void Awake()
    {
        Instance = this;
        
        videoPlayer.prepareCompleted += (VideoPlayer vp) =>
        {
            videoPlayer.Play();
            
            foreach (string startEvent in currentClipData.onClipStartEvents)
            {
                Invoke(startEvent, 0f);
            }
            
            StopCoroutine(showLoadingScreenCoroutine);
            loadingScreen.SetActive(false);
            isLoading = false;
            ShowButtonLayouts();
            subtitleIndex = 0;
            showNextSubtitleCoroutine = StartCoroutine(ShowNextSubtitle());
        };
    }

    private void Start()
    {
        StartCoroutine(GetSolutionsFile());
        
        ShowClip(mainMenuClip);
    }

    private void Update()
    {
        if (!isPaused)
        {
            Time.timeScale = gameSpeed;
            videoPlayer.playbackSpeed = gameSpeed;
            videoPlayer.SetDirectAudioVolume(0, gameVolume);
            subtitleBackground.gameObject.SetActive(showSubtitles);
        }
        else
        {
            Time.timeScale = 0;
            videoPlayer.playbackSpeed = 0;
        }
    }
    
    public void LoadPreviousGame()
    {
        StartCase(SaveLoadManager.Instance.LoadGame());
        ToggleSettingsMenu();
    }
    
    public void StartCase(int caseIndex)
    {
        SaveLoadManager.InvalidateSaveData(); //TODO Change save data, move SOs to Resources folder and use name to load
        currentCase = cases[caseIndex];
        enableSecretPassage = currentCase.startSecretPassage;
        enableSummonButler = currentCase.startSummonButler;
        enableItemCard = currentCase.startItemCard;
        enableInspectorNote = currentCase.startInspectorNote;
        nextEventIndex = 0;
        nextButlerIndex = 0;
        nextPassageClip = 0;
        eventCountdown = 3;
        availableNotes = 0;
        lastNoteAddedFrom = null;
        previousClip = null;
        availableRooms = 0;
        lastRoomAddedFrom = null;
        currentRoom = null;
        currentObservation = null;
        currentQuestion = null;

        if (showIntro)
        {
            setupClips[2] = currentCase.setupClips[0];
            setupClips[4] = currentCase.setupClips[1];
            setupClips[6] = currentCase.setupClips[2];
            setupClips[11] = currentCase.setupClips[3];

            ShowClip(setupClips[0]);
            nextSetupClip = 1;
        }
        else
        {
            ShowMenu();
        }
    }
    
    public void StartCase(SaveData saveData)
    {
        currentCase = cases[saveData.caseIndex];
        enableSecretPassage = saveData.enableSP;
        enableSummonButler = saveData.enableSB;
        enableItemCard = saveData.enableIC;
        enableInspectorNote = saveData.enableIN;
        nextEventIndex = saveData.nextEvent;
        nextButlerIndex = saveData.nextButler;
        nextPassageClip = saveData.nextPassage;
        eventCountdown = 3;
        availableNotes = saveData.notes;
        lastNoteAddedFrom = null;
        previousClip = null;
        availableRooms = saveData.rooms;
        lastRoomAddedFrom = null;
        currentRoom = null;
        currentObservation = null;
        currentQuestion = null;
        
        ShowMenu();
    }

    private IEnumerator GetSolutionsFile()
    {
        UnityWebRequest www = UnityWebRequest.Get("https://harveytucker.com/DVDCluedo/solutions.txt");
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Error: " + www.error);
        }
        else
        {
            string[] lines = www.downloadHandler.text.Split("\n");
            solutions = new List<List<int>>();
            foreach (string line in lines)
            {
                string[] parts = line.Split(",");
                List<int> solution = new List<int>();
                foreach (string part in parts)
                {
                    if (int.TryParse(part, out int result))
                    {
                        solution.Add(result);
                    }
                }

                solutions.Add(solution);
            }
        }
    }

    public void ShowClip(VideoClipData clip)
    {
        loadButton.SetActive(clip == mainMenuClip && SaveLoadManager.HasSaveData());
        
        if (clip == previousClip && (setupClips.Contains(clip) || clip.name.Contains("Setup")))
        {
            Debug.Log("repeating setup clip");
            nextSetupClip--;
        }
        
        if (previousClip != null && currentCase != null && clip == currentCase.menuClip)
        {
            SaveLoadManager.Instance.SaveGame(new SaveData(
                Array.IndexOf(cases, currentCase),
                nextPassageClip,
                nextEventIndex,
                nextButlerIndex,
                availableNotes,
                enableSecretPassage,
                enableSummonButler,
                enableItemCard,
                enableInspectorNote,
                availableRooms
            ));
        }

        if (currentClipData != null && clip != previousClip)
        {
            previousClip = currentClipData;
        }

        if (clip == previousClip)
        {
            previousClip = null;
        }

        if (clip == setupClips[11])
        {
            previousClip = clip;
        }

        mainMenu.HideButtons();
        caseMenu.HideButtons();
        passageOk.HideButtons();
        inspectorCall.HideButtons();
        menuRepeat.HideButtons();
        eventOkMenu.HideButtons();
        eventOptionMenu.HideButtons();
        setupRepeat.HideButtons();
        playerCount.HideButtons();
        selectionLetterReady.HideButtons();
        selectionLetterConfirm.HideButtons();
        resultsOk.HideButtons();
        noteInstructionsRepeat.HideButtons();
        noteNumberOk.HideButtons();
        noteSelect2.HideButtons();
        noteSelect3.HideButtons();
        roomSelect.HideButtons();
        roomAnswer.HideButtons();
        roomQuestion.HideButtons();

        if (showNextSubtitleCoroutine!= null) StopCoroutine(showNextSubtitleCoroutine);
        
        videoPlayer.Stop();

        if (currentCase != null && clip == currentCase.menuClip)
        {
            if (eventCountdown == 0)
            {
                clip = inspectorCallClip;
                eventCountdown = 3;
            }
            else
            {
                eventCountdown--;
            }
        }
        
        currentClipData = clip;

        if (currentClipData.name == "RESTARTGAME")
        {
            // TODO offer 'are you sure option'
            SaveLoadManager.InvalidateSaveData();
            SceneManager.LoadScene(0);
            return;
        }

        // if (currentClipData.name == "SKIPSETUPCLIP") UNUSED????
        // {
        //     StartCoroutine(ShowClip(setupClips[nextSetupClip]));
        //     yield break;
        // }

        HandleAudio();

        videoPlayer.url = "https://harveytucker.com/DVDCluedo/" + currentClipData.name + ".mp4";
        videoPlayer.isLooping = currentClipData.looping;
        if (loopPointHandler != null) videoPlayer.loopPointReached -= loopPointHandler;
        if (!currentClipData.looping)
        {
            if (currentClipData.nextClipID == null)
            {
                loopPointHandler = (VideoPlayer vp) =>
                {
                    foreach (string endEvent in currentClipData.onClipEndEvents)
                    {
                        Invoke(endEvent, 0f);
                    }
                };
            }
            else
            {
                loopPointHandler = (VideoPlayer vp) =>
                {
                    foreach (string endEvent in currentClipData.onClipEndEvents)
                    {
                        Invoke(endEvent, 0f);
                    }

                    ShowClip(currentClipData.nextClipID);
                };
            }
        }
        else
        {
            loopPointHandler = (VideoPlayer vp) =>
            {
                subtitleIndex = 0;
                showNextSubtitleCoroutine = StartCoroutine(ShowNextSubtitle());
            };
        }

        videoPlayer.loopPointReached += loopPointHandler;

        showLoadingScreenCoroutine = StartCoroutine(ShowLoadingScreenAfterDelay());

        try
        {
            videoPlayer.Prepare();
        }
        catch (Exception e)
        {
            Debug.LogError("ERROROROROROROR - " + e);
        }
    }
    
    public void RetryCurrentClip()
    { 
        ShowClip(currentClipData);
    }

    private IEnumerator ShowLoadingScreenAfterDelay()
    {
        yield return new WaitForSeconds(1.5f);
        loadingScreen.SetActive(true);
        isLoading = true;
    }

    private void ShowButtonLayouts()
    {
        List<VideoClipData> clipLinkIndexes = new List<VideoClipData>();

        switch (currentClipData.buttons)
        {
            case ButtonLayouts.MainMenu:
                for (int i = 0; i < 10; i++)
                {
                    clipLinkIndexes.Add(cases.Length > i ? staticButtonsClip : disabledButtonsClip);
                }
                clipLinkIndexes.Add(disabledButtonsClip); //TODO setup general case
                mainMenu.SetupButtons(clipLinkIndexes);
                break;
            case ButtonLayouts.CaseMenu:
                clipLinkIndexes.Add(enableSecretPassage ? passageStartClip : disabledButtonsClip); //TODO Add name for secret passage start clip and others below
                clipLinkIndexes.Add(enableSummonButler ? staticButtonsClip : disabledButtonsClip);
                clipLinkIndexes.Add(enableItemCard ? staticButtonsClip : disabledButtonsClip);
                clipLinkIndexes.Add(enableInspectorNote ? staticButtonsClip : disabledButtonsClip);
                clipLinkIndexes.Add(accusationStartClip); // Accusation
                clipLinkIndexes.Add(restartGameClip); // Restart
                caseMenu.SetupButtons(clipLinkIndexes);
                break;
            case ButtonLayouts.PassageOk:
                clipLinkIndexes.Add(passageEndClip);
                passageOk.SetupButtons(clipLinkIndexes);
                break;
            case ButtonLayouts.InspectorCall:
                if (nextEventIndex >= currentCase.eventClips.Length)
                {
                    clipLinkIndexes.Add(overflowEventClip);
                }
                else
                {
                    clipLinkIndexes.Add(currentCase.eventClips[nextEventIndex]);
                }

                inspectorCall.SetupButtons(clipLinkIndexes);
                nextEventIndex++;
                break;
            case ButtonLayouts.MenuRepeat:
                clipLinkIndexes.Add(currentCase.menuClip);
                clipLinkIndexes.Add(previousClip);
                menuRepeat.SetupButtons(clipLinkIndexes);
                break;
            case ButtonLayouts.EventOkMenu:
                clipLinkIndexes.Add(currentCase.eventClips[nextEventIndex]);
                nextEventIndex++;
                eventOkMenu.SetupButtons(clipLinkIndexes);
                break;
            case ButtonLayouts.EventOptionMenu:
                clipLinkIndexes.Add(currentCase.eventClips[nextEventIndex]);
                clipLinkIndexes.Add(currentCase.menuClip);
                nextEventIndex++;
                eventOptionMenu.SetupButtons(clipLinkIndexes);
                break;
            case ButtonLayouts.SetupRepeat:
                if (nextSetupClip >= setupClips.Count)
                {
                    clipLinkIndexes.Add(currentCase.introClip);
                }
                else
                {
                    clipLinkIndexes.Add(setupClips[nextSetupClip]);
                }
                clipLinkIndexes.Add(previousClip);
                nextSetupClip++;
                setupRepeat.SetupButtons(clipLinkIndexes);
                break;
            case ButtonLayouts.PlayerCount:
                clipLinkIndexes.Add(currentCase.players3Clip);
                clipLinkIndexes.Add(currentCase.players4Clip);
                clipLinkIndexes.Add(currentCase.players5Clip);
                playerCount.SetupButtons(clipLinkIndexes);
                break;
            case ButtonLayouts.SelectionLetterReady:
                clipLinkIndexes.Add(selectionLetterReadyClip);
                selectionLetterReady.SetupButtons(clipLinkIndexes);
                break;
            case ButtonLayouts.SelectionLetterConfirm:
                clipLinkIndexes.Add(staticButtonsClip);
                clipLinkIndexes.Add(previousClip);
                selectionLetterConfirm.SetupButtons(clipLinkIndexes);
                break;
            case ButtonLayouts.ResultsOk:
                if (correct == 4)
                {
                    clipLinkIndexes.Add(currentCase.endingClip);
                }
                else
                {
                    clipLinkIndexes.Add(currentCase.menuClip);
                }
                resultsOk.SetupButtons(clipLinkIndexes);
                break;
            case ButtonLayouts.NoteInstructionsRepeat:
                clipLinkIndexes.Add(staticButtonsClip);
                clipLinkIndexes.Add(previousClip);
                noteInstructionsRepeat.SetupButtons(clipLinkIndexes);
                break;
            case ButtonLayouts.NoteNumberOk:
                clipLinkIndexes.Add(currentCase.menuClip);
                noteNumberOk.SetupButtons(clipLinkIndexes);
                break;
            case ButtonLayouts.NoteSelect2:
                clipLinkIndexes.Add(staticButtonsClip);
                clipLinkIndexes.Add(staticButtonsClip);
                noteSelect2.SetupButtons(clipLinkIndexes);
                break;
            case ButtonLayouts.NoteSelect3:
                clipLinkIndexes.Add(staticButtonsClip);
                clipLinkIndexes.Add(staticButtonsClip);
                clipLinkIndexes.Add(staticButtonsClip);
                noteSelect3.SetupButtons(clipLinkIndexes);
                break;
            case ButtonLayouts.RoomSelect:
                clipLinkIndexes.Add(staticButtonsClip);
                clipLinkIndexes.Add(staticButtonsClip);
                roomSelect.SetupButtons(clipLinkIndexes);
                break;
            case ButtonLayouts.RoomAnswer:
                clipLinkIndexes.Add(currentCase.menuClip);
                clipLinkIndexes.Add(currentRoom.successClip);
                roomAnswer.SetupButtons(clipLinkIndexes);
                break;
            case ButtonLayouts.RoomQuestion:
                clipLinkIndexes.Add(currentQuestion.answerClip);
                roomQuestion.SetupButtons(clipLinkIndexes);
                break;
            default:
                break;
        }
    }

    private IEnumerator ShowNextSubtitle()
    {
        subtitleText.text = "";
        LayoutRebuilder.ForceRebuildLayoutImmediate(subtitleBackground);
        if (subtitleIndex >= currentClipData.subtitles.Count)
        {
            yield break;
        }

        yield return new WaitForSeconds(currentClipData.subtitles[subtitleIndex].startDelay);
        subtitleText.text = currentClipData.subtitles[subtitleIndex].text;
        LayoutRebuilder.ForceRebuildLayoutImmediate(subtitleBackground);
        yield return new WaitForSeconds(currentClipData.subtitles[subtitleIndex].duration);
        subtitleIndex++;
        showNextSubtitleCoroutine = StartCoroutine(ShowNextSubtitle());
    }

    public void ToggleSettingsMenu()
    {
        settingsMenu.SetActive(!settingsMenu.activeSelf);
        isPaused = !isPaused;
        if (settingsMenu.activeSelf)
        {
            if (highlightFirstButton) EventSystem.current.SetSelectedGameObject(continueButton);

            settingsButton.interactable = false;
            reloadClipButton.interactable = false;
            if (currentLayout != null) currentLayout.DisableButtonNavigation();
        }
        else if (currentLayout != null)
        {
            settingsButton.interactable = true;
            reloadClipButton.interactable = true;
            currentLayout.UpdateButtonNavigation();
        }
    }

    private void ShowMenu()
    {
        ShowClip(currentCase.menuClip);
    }

    private void HandleAudio()
    {
        if ((currentClipData.name.Contains("Menu") || currentClipData.name.Contains("Still")) && !enableMenuMusic)
        {
            videoPlayer.SetDirectAudioMute(0, true);
            return;
        }

        videoPlayer.SetDirectAudioMute(0, false);
    }

    private void Set3Players()
    {
        for (int i = 0; i < setupClips.Count; i++)
        {
            if (setupClips[i].name == "DEALCLIP")
            {
                setupClips[i] = players3DealClip;
            }
        }
    }

    private void Set4Players()
    {
        for (int i = 0; i < setupClips.Count; i++)
        {
            if (setupClips[i].name == "DEALCLIP")
            {
                setupClips[i] = players4DealClip;
            }
        }
    }

    private void Set5Players()
    {
        for (int i = 0; i < setupClips.Count; i++)
        {
            if (setupClips[i].name == "DEALCLIP")
            {
                setupClips[i] = players5DealClip;
            }
        }
    }

    private void SecretPassage()
    {
        if (nextPassageClip >= currentCase.secretPassageClips.Length)
        {
            ShowClip(overflowSecretPassageClip);
        }
        else
        {
            ShowClip(currentCase.secretPassageClips[nextPassageClip]);
            nextPassageClip++;
        }
    }

    private void EnableSecretPassage()
    {
        enableSecretPassage = true;
    }

    private void EnableButler()
    {
        enableSummonButler = true;
    }

    private void AddItemCard()
    {
        enableItemCard = true;
        if (currentClipData != lastRoomAddedFrom)
        {
            availableRooms++;
            lastRoomAddedFrom = currentClipData;
        }
    }

    private void AddInspectorNote()
    {
        enableInspectorNote = true;
        if (currentClipData != lastNoteAddedFrom)
        {
            availableNotes++;
            lastNoteAddedFrom = currentClipData;
        }
    }

    private void PickSelectionLetter()
    {
        int randomIndex = UnityEngine.Random.Range(0, SelectionManager.Instance.selectionSprites.Length - 1);
        selectionLetterImage.sprite = SelectionManager.Instance.selectionSprites[randomIndex];
        SelectionManager.Instance.currentSelectionLetter = randomIndex;
    }

    private void ShowSelectionLetter()
    {
        selectionLetterImage.gameObject.SetActive(true);
    }

    private void HideSelectionLetter()
    {
        selectionLetterImage.gameObject.SetActive(false);
    }

    public void StartSelections()
    {
        SelectionManager.Instance.isSelecting = true;
        SelectionManager.Instance.ShowNextSelectionPage();
    }

    public void ShowResults(List<int> selections)
    {
        correct = 0;
        for (int i = 0; i < selections.Count; i++)
        {
            if (solutions[Array.IndexOf(cases, currentCase)][i] == selections[i])
            {
                correct++;
            }
        }

        ShowClip(resultsClips[correct]);
    }

    public void ShowButlerClip()
    {
        if (nextButlerIndex >= currentCase.butlerClips.Length)
        {
            ShowClip(overflowButlerClip);
        }
        else
        {
            ShowClip(currentCase.butlerClips[nextButlerIndex]);
            nextButlerIndex++;
        }
    }

    public void StartNotes()
    {
        int iNum = UnityEngine.Random.Range(0, noteInstructionClips.Length);
        ShowClip(noteInstructionClips[iNum]);
    }

    public void CheckForNoteMenu()
    {
        if (availableNotes == 1) ShowNoteNumberClip(0);
        else ShowClip(noteMenuClips[availableNotes - 2]);
    }

    public void ShowNoteNumberClip(int noteIndex)
    {
        noteNumberImage.sprite = currentCase.noteNumberSprites[noteIndex];
        ShowClip(noteNumberClip);
    }

    private void ShowNoteNumber()
    {
        noteNumberImage.gameObject.SetActive(true);
    }
    
    public void HideNoteNumber()
    {
        noteNumberImage.gameObject.SetActive(false);
    }

    public void CheckForRoomMenu()
    {
        if (availableRooms == 1) ShowRoomClip(0);
        else ShowClip(currentCase.roomMenu);
    }
    
    public void ShowRoomClip(int roomIndex)
    {
        currentRoom = currentCase.observableRooms[roomIndex];
        RoomItemManager.Instance.GetRandomQuestionFromRoom(currentRoom, out currentObservation, out currentQuestion);
        ShowClip(currentObservation.observationClip);
    }

    private void ShowRoomQuestion()
    {
        ShowClip(currentQuestion.questionClip);
    }
    
    public void StopGame()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}

public enum ButtonLayouts
{
    None,
    MainMenu,
    CaseMenu,
    PassageOk,
    InspectorCall,
    MenuRepeat,
    EventOkMenu,
    EventOptionMenu,
    SetupRepeat,
    PlayerCount,
    SelectionLetterReady,
    SelectionLetterConfirm,
    ResultsOk,
    NoteInstructionsRepeat,
    NoteNumberOk,
    NoteSelect2,
    NoteSelect3,
    RoomSelect,
    RoomAnswer,
    RoomQuestion
}
