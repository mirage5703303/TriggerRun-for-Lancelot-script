using System;
using IEdgeGames;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The inventory slot can either be an item or an hero
/// </summary>
public class InventorySlotEntry : MonoBehaviour
{
    public bool IsHeroSlot;
    [ShowIf("@!IsHeroSlot")]
    public ItemSlotCategory ItemSlotCategory;

    public Button ClickButton;
    public RectTransform AssignedRoot;
    public RectTransform UnassignedRoot;

    [Header("Hero")]
    public TextMeshProUGUI HeroText;
    public Image HeroIcon;
    public bool ShowHeroText;

    [Header("Item")]
    public TextMeshProUGUI ItemText;
    public Image ItemIcon;
    public bool ShowItemText;

    private Action<InventorySlotEntry> _callback;

    public TriggeRunHeroData HeroData { get; set; }
    public TriggeRunItemData ItemData { get; set; }
    public int SkinIndex { get; set; }

    private void Awake()
    {
        ClickButton.onClick.AddListener(() => _callback?.Invoke(this));
    }

    public void SetHero(TriggeRunHeroData data, int skinIndex,  Action<InventorySlotEntry> callback)
    {
        HeroData = data;
        ItemData = null;
        SkinIndex = skinIndex;
        _callback = callback;

        ItemIcon.gameObject.SetActive(false);
        ItemText.gameObject.SetActive(false);
        
        HeroIcon.gameObject.SetActive(true);
        HeroText.gameObject.SetActive(ShowHeroText);
        
        HeroText.text = data.GetHero()?.name ?? "Null";
        HeroIcon.sprite = data.GetHero()?.GetSkinByIndex(skinIndex);

        UnassignedRoot.gameObject.SetActive(false);
        AssignedRoot.gameObject.SetActive(true); 
    }

    public void SetItem(TriggeRunItemData data, int skinIndex, Action<InventorySlotEntry> callback)
    {
        ItemData = data;
        HeroData = null;
        SkinIndex = skinIndex;
        _callback = callback;

        HeroIcon.gameObject.SetActive(false);
        HeroText.gameObject.SetActive(false);
        
        ItemIcon.gameObject.SetActive(true);
        ItemText.gameObject.SetActive(ShowItemText);

        ItemText.text = data.GetItem()?.Name ?? "Null";
        ItemIcon.sprite = data.GetItem()?.GetSkinByIndex(skinIndex);
        
        UnassignedRoot.gameObject.SetActive(false);
        AssignedRoot.gameObject.SetActive(true);
    }

    public void Unassign(Action<InventorySlotEntry> callback)
    {
        UnassignedRoot.gameObject.SetActive(true);
        AssignedRoot.gameObject.SetActive(false);

        ItemData = null;
        HeroData = null;
        _callback = callback;
    }

    public bool IsAssigned()
    {
        return ItemData != null || HeroData != null;
    }

    public void RefreshView()
    {
        if (ItemData != null)
        {
            var preset = TriggeRunGameManager.Instance.CurrentPlayerData.GetPreset();
            int findIndex = preset.ItemIds.IndexOf(ItemData.ItemId);
            SetItem(ItemData, preset.ItemSkinIndices[findIndex], _callback);
        }
        else if (HeroData != null)
        {
            var preset = TriggeRunGameManager.Instance.CurrentPlayerData.GetPreset();
            SetHero(HeroData, preset.HeroSkinIndex, _callback);
        }
    }
}