using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using TrickCore;
using UnityEngine;
using UnityEngine.UI;
using HighlightState = TrickCore.HighlightState;

public class InventoryPresetSlotEntry : MonoBehaviour
{
    public List<Sprite> InventorySprites;
    public Image BackpackIcon;
    public Button ClickButton;
    public TextMeshProUGUI NumItemsEquippedText;
    private Action<InventoryPresetSlotEntry> _action;
    public int CurrentIndex { get; set; }

    public RectTransform Locked;
    public RectTransform Unlocked;
    
    public TriggeRunPresetData CurrentPreset { get; set; }

    private void Awake()
    {
        ClickButton.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        UIManager.Instance.PlayButtonAudio();
        
        _action?.Invoke(this);
    }

    public void SetData(TriggeRunPresetData preset, int index, Action<InventoryPresetSlotEntry> action)
    {
        CurrentPreset = preset;
        CurrentIndex = index;
        _action = action;

        int numItems = preset != null ? preset.ItemIds.Count : 0;
        NumItemsEquippedText.text = numItems.ToString();
        NumItemsEquippedText.gameObject.SetActive(numItems > 0);
        
        Unlocked.gameObject.SetActive(preset != null);
        Locked.gameObject.SetActive(preset == null);

        if (preset != null)
        {
            TrickVisualHelper.SetHighlighted(this, null, TriggeRunGameManager.Instance.CurrentPlayerData.selectedPresetIndex == index ? HighlightState.AlwaysOn : HighlightState.Off);
        }
        else
        {
            TrickVisualHelper.SetHighlighted(this, null, HighlightState.Off);
        }
        
        BackpackIcon.sprite = index >= 0 && index < InventorySprites.Count
            ? InventorySprites[index]
            : InventorySprites.FirstOrDefault();
    }

}