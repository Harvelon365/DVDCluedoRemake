using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SelectionManager : MonoBehaviour
{
    public static SelectionManager Instance;
    
    private int currentPageIndex;
    private Image[] currentPageImages;
    public bool isSelecting;
    public Sprite[] selectionSprites;
    [SerializeField] private GameObject[] selectionPageButtons;
    [SerializeField] private VideoClipData[] selectionPageClips;
    public int currentSelectionLetter;
    [SerializeField] private int currentSelectionIndex;
    private List<int> selections = new List<int>();

    [SerializeField] private InputActionReference moveLeftAction;
    [SerializeField] private InputActionReference moveRightAction;
    [SerializeField] private InputActionReference selectAction;

    private void OnEnable()
    {
        moveLeftAction.action.performed += MoveSelectionLeft;
        moveRightAction.action.performed += MoveSelectionRight;
        selectAction.action.performed += ShowNextSelectionPage;
    }
    
    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        currentPageIndex = -1;
        currentSelectionLetter = -10;
    }

    private void MoveSelectionRight(InputAction.CallbackContext obj)
    {
        if (!isSelecting) return;
        Sprite tempSprite = currentPageImages[^1].sprite;
        for (int i = currentPageImages.Length - 1; i > 0; i--)
        {
            currentPageImages[i].sprite = currentPageImages[i - 1].sprite;
        }
        currentPageImages[0].sprite = tempSprite;

        currentSelectionIndex++;
        if (currentSelectionIndex >= currentPageImages.Length)
        {
            currentSelectionIndex = 0;
        }
    }
    
    private void MoveSelectionLeft(InputAction.CallbackContext obj)
    {
        if (!isSelecting) return;
        var tempSprite = currentPageImages[0].sprite;
        for (var i = 0; i < currentPageImages.Length - 1; i++)
        {
            currentPageImages[i].sprite = currentPageImages[i + 1].sprite;
        }
        currentPageImages[^1].sprite = tempSprite;
        
        currentSelectionIndex--;
        if (currentSelectionIndex < 0)
        {
            currentSelectionIndex = currentPageImages.Length - 1;
        }
    }

    public void ShowNextSelectionPage(InputAction.CallbackContext obj = default)
    {
        if (!isSelecting) return;
        StartCoroutine(CoShowNextSelectionPage());
    }

    private IEnumerator CoShowNextSelectionPage()
    {
        if (currentPageIndex > -1)
        {
            selectionPageButtons[currentPageIndex].SetActive(false);
            selections.Add(currentSelectionIndex);
        }
        else
        {
            selections = new List<int>();
        }
        currentPageIndex++;
        if (currentPageIndex >= selectionPageButtons.Length)
        {
            currentPageIndex = -1;
            FinishSelections();
            yield break;
        }
        GameManager.Instance.ShowClip(selectionPageClips[currentPageIndex]);
        
        currentPageImages = selectionPageButtons[currentPageIndex].GetComponentsInChildren<Image>();

        for (var i = 0; i < currentPageImages.Length; i++)
        {
            currentPageImages[i].sprite = selectionSprites[i];
        }

        isSelecting = true;
        currentSelectionIndex = currentSelectionLetter;

        yield return new WaitUntil(() => GameManager.Instance.videoPlayer.isPrepared);
        
        selectionPageButtons[currentPageIndex].SetActive(true);
    }
    
    public void FinishSelections()
    {
        isSelecting = false;
        currentSelectionLetter = -10;
        GameManager.Instance.ShowResults(selections);
    }
}
