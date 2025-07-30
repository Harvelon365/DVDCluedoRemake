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
        navigableButtons.Add(new SelectableButton(GameManager.Instance.settingsButton, new Vector2(3, 6)));
    }

    public void SetupButtons(List<VideoClipData> clipLinkIndexes)
    {
        GameManager.Instance.currentLayout = this;
        
        for (var i = 0; i < buttons.Length; i++)
        {
            buttons[i].button.onClick.RemoveAllListeners();
            
            var clipLinkIndex = clipLinkIndexes[i];

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
        for (var i = 0; i < navigableButtons.Count; i++)
        {
            var current = navigableButtons[i];
            if (!current.button.interactable) continue;
            
            var nav = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnUp = GetNearestSelectable(i, Vector2.down),
                selectOnDown = GetNearestSelectable(i, Vector2.up),
                selectOnLeft = GetNearestSelectable(i, Vector2.left),
                selectOnRight = GetNearestSelectable(i, Vector2.right)
            };

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
        var fromButton = navigableButtons[fromIndex];
        var fromPosition = fromButton.gridPosition;
        
        var bestScore = float.MaxValue;
        SelectableButton bestButton = null;
        
        for (var i = 0; i < navigableButtons.Count; i++)
        {
            if (i == fromIndex) continue;
            
            var candidate = navigableButtons[i];
            if (candidate.button == null) continue;
            if (!candidate.button.interactable) continue;
            if (!candidate.button.gameObject.activeInHierarchy) continue;
            
            var toPosition = candidate.gridPosition;
            var vectorToTarget = (toPosition - fromPosition).normalized;
            var dot = Vector2.Dot(vectorToTarget, direction);
            var distance = Vector2.Distance(fromPosition, toPosition);

            if (!(dot > 0.7f) || !(distance < bestScore)) continue;
            bestScore = distance;
            bestButton = candidate;
        }

        return bestButton?.button;
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