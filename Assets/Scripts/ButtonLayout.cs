using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonLayout : MonoBehaviour
{
    public SelectableButton[] buttons;
    private List<SelectableButton> navigableButtons;

    private void Start()
    {
        navigableButtons = buttons.ToList();
        navigableButtons.Add(new SelectableButton(GameManager.Instance.reloadClipButton, new Vector2(3, 5)));
        navigableButtons.Add(new SelectableButton(GameManager.Instance.settingsButton, new Vector2(3, 6)));;
    }

    public void SetupButtons(List<VideoClipData> clipLinkIndexes)
    {
        GameManager.Instance.currentLayout = this;
        
        for (int i = 0; i < buttons.Length; i++)
        {
            buttons[i].button.onClick.RemoveAllListeners();
            
            VideoClipData clipLinkIndex = clipLinkIndexes[i];

            switch (clipLinkIndex.name)
            {
                case "DISABLEDBUTTONS":
                    buttons[i].button.interactable = false;
                    buttons[i].button.image.enabled = true;
                    break;
                case "STATICBUTTONS":
                    buttons[i].button.interactable = true;
                    buttons[i].button.image.enabled = true;
                    break;
                default:
                    buttons[i].button.interactable = true;
                    buttons[i].button.image.enabled = true;
                    buttons[i].button.onClick.AddListener(() => GameManager.Instance.ShowClip(clipLinkIndex));
                    break;
            }
        }
        
        UpdateButtonNavigation();
    }

    public void HideButtons()
    {
        foreach (var button in buttons)
        {
            if (button.button == null) continue;
            button.button.interactable = false;
            button.button.image.enabled = false;
        }
    }
    
    public void UpdateButtonNavigation()
    {
        for (int i = 0; i < navigableButtons.Count; i++)
        {
            SelectableButton current = navigableButtons[i];
            if (!current.button.interactable) continue;
            
            Navigation nav = new Navigation { mode = Navigation.Mode.Explicit };
            nav.selectOnUp = GetNearestSelectable(i, Vector2.down);
            nav.selectOnDown = GetNearestSelectable(i, Vector2.up);
            nav.selectOnLeft = GetNearestSelectable(i, Vector2.left);
            nav.selectOnRight = GetNearestSelectable(i, Vector2.right);
            
            current.button.navigation = nav;
        }

        HighlightButton(GameManager.Instance.highlightFirstButton);
    }

    public void HighlightButton(bool highlight)
    {
        if (highlight) EventSystem.current.SetSelectedGameObject(buttons[0].button.gameObject);
    }

    private Selectable GetNearestSelectable(int fromIndex, Vector2 direction)
    {
        SelectableButton fromButton = navigableButtons[fromIndex];
        Vector2 fromPosition = fromButton.gridPosition;
        
        float bestScore = float.MaxValue;
        SelectableButton bestButton = null;
        
        for (int i = 0; i < navigableButtons.Count; i++)
        {
            if (i == fromIndex) continue;
            
            SelectableButton candidate = navigableButtons[i];
            if (candidate.button == null) continue;
            if (!candidate.button.interactable) continue;
            if (!candidate.button.gameObject.activeInHierarchy) continue;
            
            Vector2 toPosition = candidate.gridPosition;
            Vector2 vectorToTarget = (toPosition - fromPosition).normalized;
            float dot = Vector2.Dot(vectorToTarget, direction);
            float distance = Vector2.Distance(fromPosition, toPosition);
            
            if (dot > 0.7f && distance < bestScore)
            {
                bestScore = distance;
                bestButton = candidate;
            }
        }

        return bestButton?.button;;
    }

    public void DisableButtonNavigation()
    {
        foreach (SelectableButton button in buttons)
        {
            if (button != null)
            {
                button.button.navigation = new Navigation { mode = Navigation.Mode.None };
            }
        }
    }
}

[Serializable]
public class SelectableButton
{
    public Button button;
    public Vector2 gridPosition;

    public SelectableButton(Button button, Vector2 position)
    {
        this.button = button;
        this.gridPosition = position;
    }
}