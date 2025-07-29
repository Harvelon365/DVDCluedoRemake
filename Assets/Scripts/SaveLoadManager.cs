using System.Collections;
using UnityEngine;

public class SaveLoadManager : MonoBehaviour
{
    public static SaveLoadManager Instance;
    [SerializeField] private CanvasGroup saveText;
    
    private void Awake()
    {
        Instance = this;
    }
    
    public void SaveGame(SaveData saveData)
    {
        StartCoroutine(ShowSaveText());
        
        PlayerPrefs.SetInt("ValidSaveData", 1);
        PlayerPrefs.SetInt("Case", saveData.caseIndex);
        PlayerPrefs.SetInt("NextPassage", saveData.nextPassage);
        PlayerPrefs.SetInt("NextEvent", saveData.nextEvent);
        PlayerPrefs.SetInt("NextButler", saveData.nextButler);
        PlayerPrefs.SetInt("Notes", saveData.notes);
        PlayerPrefs.SetInt("EnableSP", saveData.enableSP ? 1 : 0);
        PlayerPrefs.SetInt("EnableSB", saveData.enableSB ? 1 : 0);
        PlayerPrefs.SetInt("EnableIC", saveData.enableIC ? 1 : 0);
        PlayerPrefs.SetInt("EnableIN", saveData.enableIN ? 1 : 0);
        PlayerPrefs.SetInt("Rooms", saveData.rooms);
    }

    private IEnumerator ShowSaveText()
    {
        while (saveText.alpha < 1f)
        {
            saveText.alpha += Time.deltaTime * 2f;
            yield return null;
        }
        yield return new WaitForSeconds(1f);
        while (saveText.alpha > 0f)
        {
            saveText.alpha -= Time.deltaTime * 2f;
            yield return null;
        }
    }

    public SaveData LoadGame()
    {
        SaveData saveData = new SaveData(
            PlayerPrefs.GetInt("Case", 0),
            PlayerPrefs.GetInt("NextPassage", 0),
            PlayerPrefs.GetInt("NextEvent", 0),
            PlayerPrefs.GetInt("NextButler", 0),
            PlayerPrefs.GetInt("Notes", 0),
            PlayerPrefs.GetInt("EnableSP", 0) == 1,
            PlayerPrefs.GetInt("EnableSB", 0) == 1,
            PlayerPrefs.GetInt("EnableIC", 0) == 1,
            PlayerPrefs.GetInt("EnableIN", 0) == 1,
            PlayerPrefs.GetInt("Rooms", 0)
        );

        return saveData;
    }
    
    public static void InvalidateSaveData()
    {
        //Debug.Log("Invalidating save data");
        PlayerPrefs.SetInt("ValidSaveData", 0);
    }
    
    public static bool HasSaveData()
    {
        return PlayerPrefs.GetInt("ValidSaveData", 0) == 1;
    }
}

public class SaveData
{
    public int caseIndex;
    public int nextPassage;
    public int nextEvent;
    public int nextButler;
    public int notes;
    public bool enableSP;
    public bool enableSB;
    public bool enableIC;
    public bool enableIN;
    public int rooms;

    public SaveData(int caseIndex, int nextPassage, int nextEvent, int nextButler, int notes, bool enableSP,
        bool enableSB, bool enableIC, bool enableIN, int rooms)
    {
        this.caseIndex = caseIndex;
        this.nextPassage = nextPassage;
        this.nextEvent = nextEvent;
        this.nextButler = nextButler;
        this.notes = notes;
        this.enableSP = enableSP;
        this.enableSB = enableSB;
        this.enableIC = enableIC;
        this.enableIN = enableIN;
        this.rooms = rooms;
    }
}
