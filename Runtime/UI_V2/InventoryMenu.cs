using System;
using System.Collections.Generic;
using System.Linq;
using IEdgeGames;
using TrickCore;
using UnityEngine;
using UnityEngine.UI;

public class InventoryMenu : UIMenu
{
    [Header("Hero Inventory")]
    public ShopHeroEntry ShopHeroEntryPrefab;
    public RectTransform ShopHeroEntryContentTransform;
    public RectTransform HeroInventoryView;
    
    [Header("Item Inventory")]
    public ShopItemEntry ShopItemEntryPrefab;
    public RectTransform ShopItemEntryContentTransform;
    public RectTransform ItemInventoryView;
    public GridLayoutGroup ItemInventoryGrid;
    public Dictionary<ItemSlotCategory, Vector2> GridSizeByCategory;
    public Dictionary<ItemSlotCategory, int> GridConstraintsByCategory;

    [Header("Presets")]
    public InventoryPresetSlotEntry PresetSlotEntryPrefab;
    public RectTransform PresetSlotEntryContentTransform;

    [Header("Others")]
    public Transform AvatarRoot;

    public SlotInfoPanel SlotInfoPanel;
    
    public RectTransform InventorySlotsRoot;
    public List<InventorySlotEntry> InventorySlotEntries { get; private set; }

    private List<ShopHeroEntry> _heroEntries = new List<ShopHeroEntry>();
    private List<ShopItemEntry> _itemEntries = new List<ShopItemEntry>();
    private GameObject _lastCharacter;
    private CharacterDefinition _lastCharacterDef;
    private ShopHeroEntry _currentSelectedHeroEntry;
    private ShopItemEntry _currentSelectedItemEntry;
    private (bool autoSelect, ItemSlotCategory category, Func<ItemDefinition,bool> predicate) _lastItemShopInventoryViewData;
    private (bool autoSelect, Func<CharacterDefinition,bool> predicate) _lastHeroShopInventoryViewData;
    private InventorySlotEntry _currentSelectedItemInventoryEntry;
    private List<InventoryPresetSlotEntry> _presets = new List<InventoryPresetSlotEntry>();
    private int? _lastHeroPreviewId;
    private int? _lastItemPreviewId;

    protected override void AddressablesAwake()
    {
        base.AddressablesAwake();

        InventorySlotEntries = InventorySlotsRoot.GetComponentsInChildren<InventorySlotEntry>(true).ToList();
        InventorySlotEntries.ForEach(entry => entry.Unassign(null));
        SlotInfoPanel.gameObject.SetActive(false);
    }

    public override UIMenu Show()
    {
        UpdateRightSideView();

        UpdateInventoryPreset();
        
        UpdateCharacterView(null, null);

        UpdateHeroShopInventoryView(true);
        
        return base.Show();
    }

    private void UpdateItemShopInventoryView(bool autoSelect, ItemSlotCategory category, Func<ItemDefinition,bool> predicate = null)
    {
        _lastItemShopInventoryViewData = (autoSelect, category, predicate);
        TriggeRunGameManager.Instance.ItemDefinitions.Where(definition => !definition.Disabled)
            .Where(predicate ?? (definition => true))
            .Select(definition =>
            {
                (bool HasItem, ItemDefinition Item) x = (TriggeRunGameManager.Instance.CurrentPlayerData.items.Any(data => data.ItemId == definition.id), definition);
                return x;
            })
            .OrderByDescending(tuple => tuple.HasItem)
            .ThenByDescending(tuple => tuple.Item.GemCosts == 0 ? int.MaxValue : tuple.Item.GemCosts)
            .ThenByDescending(tuple => tuple.Item.ReleaseDate)
            .PoolGetOrInstantiate(ref _itemEntries, (entry, tuple, arg3) =>
            {
                entry.SetData(tuple.Item, OnItemEntryClicked);
            }, (definition, i) => Instantiate(ShopItemEntryPrefab, ShopItemEntryContentTransform));
        
        if (autoSelect)
        {
            var preset = TriggeRunGameManager.Instance.CurrentPlayerData.GetPreset();

            var otherSlots = _currentSelectedItemInventoryEntry != null ? InventorySlotEntries.FindAll(entry => !entry.IsHeroSlot &&
                entry.ItemSlotCategory == _currentSelectedItemInventoryEntry.ItemSlotCategory) : new List<InventorySlotEntry>();
            
            // Try selecting the current hero the player has equipped, otherwise a hero the player has in their inventory
            ShopItemEntry first = null;
            foreach (var itemEntry in _itemEntries.Where(entry =>
                     {
                         if (_currentSelectedItemInventoryEntry != null)
                         {

                             if (!_currentSelectedItemInventoryEntry.IsAssigned())
                             {
                                 var ids = otherSlots.Where(slotEntry => slotEntry != null && slotEntry.IsAssigned())
                                     .Select(slotEntry => slotEntry.ItemData?.ItemId ?? 0)
                                     .Where(i => i != 0)
                                     .ToList();
                                 if (ids.Count > 0) return !ids.Contains(entry.Item.id);
                             }
                             else
                             {
                                 return entry.Item.id == _currentSelectedItemInventoryEntry.ItemData.ItemId;
                             }
                         }

                         return preset.ItemIds.Contains(entry.Item.id);
                     }))
            {
                first = itemEntry;
                break;
            }

            if (first is { } equippedItem)
            {
                OnItemEntryClicked(equippedItem);

                ItemInventoryView.GetComponent<ScrollRect>().FocusOnItem(equippedItem.transform as RectTransform);
            }
            else if (_itemEntries.FirstOrDefault(entry => entry.Item != null) is { } firstItem)
            {
                OnItemEntryClicked(firstItem);
                
                ItemInventoryView.GetComponent<ScrollRect>().FocusOnItem(firstItem.transform as RectTransform);
            }
            
        }

        if (GridSizeByCategory.TryGetValue(category, out var size) &&
            GridConstraintsByCategory.TryGetValue(category, out var constraints))
        {
            ItemInventoryGrid.cellSize = size;
            ItemInventoryGrid.constraintCount = constraints;
        }
        HeroInventoryView.gameObject.SetActive(false);
        ItemInventoryView.gameObject.SetActive(true);
    }
    private void UpdateHeroShopInventoryView(bool autoSelect, Func<CharacterDefinition,bool> predicate = null)
    {
        _lastHeroShopInventoryViewData = (autoSelect, predicate);
        // Show our heroes
        TriggeRunGameManager.Instance.CharacterDefinitions.Where(definition => !definition.disabled)
            .Where(predicate ?? (definition => true))
            .Select(definition =>
            {
                (bool HasHero, CharacterDefinition Character) x = (TriggeRunGameManager.Instance.CurrentPlayerData.heroes.Any(data => data.HeroId == definition.id), definition);
                return x;
            })
            .OrderByDescending(tuple => tuple.HasHero)
            .ThenByDescending(tuple => tuple.Character.GemCosts == 0 ? int.MaxValue : tuple.Character.GemCosts)
            .ThenByDescending(tuple => tuple.Character.ReleaseDate)
            .PoolGetOrInstantiate(ref _heroEntries, (entry, tuple, arg3) =>
            {
                entry.SetData(tuple.Character, tuple.HasHero, OnHeroEntryClicked);
            }, (definition, i) => Instantiate(ShopHeroEntryPrefab, ShopHeroEntryContentTransform));

        if (autoSelect)
        {
            var currentCharacter = TriggeRunGameManager.Instance.GetCurrentCharacter();

            // Try selecting the current hero the player has equipped, otherwise a hero the player has in their inventory
            if (_heroEntries.FirstOrDefault(entry => entry.Hero == currentCharacter) is { } selectedHero)
            {
                OnHeroEntryClicked(selectedHero);
                HeroInventoryView.GetComponent<ScrollRect>().FocusOnItem(selectedHero.transform as RectTransform);
            }
            else if (_heroEntries.FirstOrDefault(entry => entry.HasHero) is { } firstHeroInInventory)
            {
                OnHeroEntryClicked(firstHeroInInventory);
                HeroInventoryView.GetComponent<ScrollRect>().FocusOnItem(firstHeroInInventory.transform as RectTransform);
            }
        }
        
        HeroInventoryView.gameObject.SetActive(true);
        ItemInventoryView.gameObject.SetActive(false);
    }
    private void UpdateRightSideView()
    {
        var preset = TriggeRunGameManager.Instance.CurrentPlayerData.GetPreset();
        
        // show hero
        var heroSlot = InventorySlotEntries.Find(entry => entry.IsHeroSlot);
        var selectedHeroData = TriggeRunGameManager.Instance.CurrentPlayerData.heroes.Find(data => data.HeroId == preset.HeroId);
        heroSlot.SetHero(selectedHeroData, preset.HeroSkinIndex, OnSelectHero);
        
        // Show our items
        var equippedItems = TriggeRunGameManager.Instance.CurrentPlayerData.items.Where(data => preset.ItemIds.Contains(data.ItemId))
            .Select(data => data).ToList();

        var hashSet = new HashSet<InventorySlotEntry>(InventorySlotEntries.Where(entry => !entry.IsHeroSlot));

        foreach (var inventorySlotGrouping in InventorySlotEntries.Where(entry => !entry.IsHeroSlot).GroupBy(entry => entry.ItemSlotCategory))
        {
            var slots = inventorySlotGrouping.ToList();
            var equipSlots = equippedItems.FindAll(data => inventorySlotGrouping.Key.HasFlag(data.GetItem().SlotCategory));
            for (var index = 0; index < slots.Count; index++)
            {
                InventorySlotEntry entry = slots[index];

                if (index >= 0 && index < equipSlots.Count)
                {
                    TriggeRunItemData equippedItem = equipSlots[index];
                    int findIndex = preset.ItemIds.IndexOf(equippedItem.ItemId);
                    entry.SetItem(equippedItem, preset.ItemSkinIndices[findIndex], OnSelectItem);
                }
                else
                {
                    entry.Unassign(OnSelectItem);
                }

                hashSet.Remove(entry);
            }
        }
        
        

        // untouched slots, unassign them
        foreach (InventorySlotEntry entry in hashSet)
        {
            entry.Unassign(OnSelectItem);
        }
    }

    private void OnSelectHero(InventorySlotEntry obj)
    {
        InventorySlotEntries.ForEach(entry => TrickVisualHelper.SetHighlighted(entry, null, entry == obj ? HighlightState.AlwaysOn : HighlightState.Off));

        HeroInventoryView.gameObject.SetActive(true);
        ItemInventoryView.gameObject.SetActive(false);
        
        _currentSelectedItemInventoryEntry = obj;
        
        SlotInfoPanel.SetHero(obj.HeroData?.GetHero());
        UpdateHeroShopInventoryView(true);

    }

    private void OnSelectItem(InventorySlotEntry obj)
    {
        InventorySlotEntries.ForEach(entry => TrickVisualHelper.SetHighlighted(entry, null, entry == obj ? HighlightState.AlwaysOn : HighlightState.Off));
        
        HeroInventoryView.gameObject.SetActive(false);
        ItemInventoryView.gameObject.SetActive(true);
        _currentSelectedItemInventoryEntry = obj;
        SlotInfoPanel.SetItem(obj.ItemData?.GetItem(), _currentSelectedItemEntry, obj);
        UpdateItemShopInventoryView(true, obj.ItemSlotCategory, definition => obj.ItemSlotCategory.HasFlag(definition.SlotCategory) && (obj.ItemData != null && definition.id == obj.ItemData.ItemId || !definition.HasEquipped()));
    }

    private void UpdateCharacterView(int? heroIdPreview, int? itemIdPreview)
    {
        var currentCharacter = heroIdPreview != null ? TriggeRunGameManager.Instance.GetCharacterById(heroIdPreview.Value) : TriggeRunGameManager.Instance.GetCurrentCharacter();
        if (currentCharacter != null && currentCharacter != _lastCharacterDef)
        {
            if (_lastCharacter != null) Destroy(_lastCharacter);
            (_lastCharacter = Instantiate(currentCharacter.uiPrefabDirect, AvatarRoot)).SetLayerRecursively("UI");
            _lastCharacterDef = currentCharacter;
        }

        _lastHeroPreviewId = heroIdPreview;
        _lastItemPreviewId = itemIdPreview;

        // todo this also needs to update their weapon
    }

    private void OnItemEntryClicked(ShopItemEntry shopItemEntry)
    {
        _itemEntries.ForEach(entry => TrickVisualHelper.SetHighlighted(entry, null, entry == shopItemEntry ? HighlightState.AlwaysOn : HighlightState.Off));
        
        SlotInfoPanel.SetItem(shopItemEntry.Item, shopItemEntry, _currentSelectedItemInventoryEntry);
        
        if (_currentSelectedItemEntry != null) _currentSelectedItemEntry.RefreshView();
        _currentSelectedItemEntry = shopItemEntry;
        
        if (_lastHeroPreviewId != null)
        {
            _lastHeroPreviewId = null;
            UpdateCharacterView(null, null);
        }
        
        UpdateRightSideView();
    }
    private void OnHeroEntryClicked(ShopHeroEntry shopHeroEntry)
    {
        _heroEntries.ForEach(entry => TrickVisualHelper.SetHighlighted(entry, null, entry == shopHeroEntry ? HighlightState.AlwaysOn : HighlightState.Off));
        
        SlotInfoPanel.SetHero(shopHeroEntry.Hero);
        
        var preset = TriggeRunGameManager.Instance.CurrentPlayerData.GetPreset();
        if (preset != null && shopHeroEntry.Hero.HasOwned())
        {
            if (preset.HeroId != shopHeroEntry.Hero.id)
            {
                preset.HeroId = shopHeroEntry.Hero.id;
                TriggeRunGameManager.Instance.SetAndFetchPublicClientStatsVoid(new Dictionary<string, string>()
                {
                    {
                        nameof(TriggeRunPlayerData.presets),
                        TriggeRunGameManager.Instance.CurrentPlayerData.presets.SerializeToJson(false, true)
                    }
                }, false);

                UpdateCharacterView(null, null);
            }
        }
        else
        {
            UpdateCharacterView(shopHeroEntry.Hero.id, null);
        }
        
        if (_currentSelectedHeroEntry != null) _currentSelectedHeroEntry.RefreshView();
        _currentSelectedHeroEntry = shopHeroEntry;
        
        UpdateRightSideView();
    }

    public override void Hide()
    {
        base.Hide();
        
        TriggeRunGameManager.Instance.FetchPublicClientStatsVoid();
        
        UIManager.Instance.TryShow<MainMenu>();
    }

    public void RefreshView()
    {
        UpdateInventoryPreset();
        
        UpdateCharacterView(null, null);
        
        UpdateRightSideView();

        if (HeroInventoryView.gameObject)
        {
            UpdateHeroShopInventoryView(false, _lastHeroShopInventoryViewData.predicate);
        }
        
        if (ItemInventoryView.gameObject)
        {
            UpdateItemShopInventoryView(false, _lastItemShopInventoryViewData.category, _lastItemShopInventoryViewData.predicate);
        }

        SlotInfoPanel.RefreshView();
    }

    private void UpdateInventoryPreset()
    {
        TriggeRunGameManager.Instance.MaxNumPresets.PoolGetOrInstantiate(ref _presets, (entry, i) =>
        {
            var preset = i >= 0 && i < TriggeRunGameManager.Instance.CurrentPlayerData.presets.Count
                ? TriggeRunGameManager.Instance.CurrentPlayerData.presets[i]
                : null;
            entry.SetData(preset, i, OnSelectPreset);
        }, i => Instantiate(PresetSlotEntryPrefab, PresetSlotEntryContentTransform));
    }

    public void SelectItemById(int itemID)
    {
        if (!(_itemEntries.FirstOrDefault(entry => entry.Item.id == itemID) is { } selectedItem)) return;
        OnItemEntryClicked(selectedItem);
        HeroInventoryView.GetComponent<ScrollRect>().FocusOnItem(selectedItem.transform as RectTransform);
    }

    public void SelectHeroById(int heroID)
    {
        if (!(_heroEntries.FirstOrDefault(entry => entry.Hero.id == heroID) is { } selectedHero)) return;
        OnHeroEntryClicked(selectedHero);
        HeroInventoryView.GetComponent<ScrollRect>().FocusOnItem(selectedHero.transform as RectTransform);
    }
    
    public void OnSelectPreset(InventoryPresetSlotEntry obj)
    {
        if (obj.CurrentPreset != null)
        {
            if (TriggeRunGameManager.Instance.CurrentPlayerData.selectedPresetIndex != obj.CurrentIndex)
            {
                _presets.ForEach(entry => entry.ClickButton.interactable = false);
                TriggeRunGameManager.Instance.CurrentPlayerData.selectedPresetIndex = obj.CurrentIndex;
            
                TriggeRunGameManager.Instance.SetAndFetchPublicClientStatsVoid(new Dictionary<string, string>
                {
                    [nameof(TriggeRunPlayerData.selectedPresetIndex)] = TriggeRunGameManager.Instance.CurrentPlayerData.selectedPresetIndex.ToString()
                }, true, () =>
                {
                    _presets.ForEach(entry => entry.ClickButton.interactable = true);
                    RefreshView();
                });
            }
            
        }
        else
        {
            // show purchase popup
            ModalPopupMenu.ShowYesNoModal("Purchase Preset", $"Do you want to purchase a new preset for {TriggeRunGameManager.Instance.PresetCosts[obj.CurrentIndex].ToStringColor(Color.yellow)} gems?", "Purchase", "No",
                () =>
                {
                    if (TriggeRunGameManager.Instance.TryConsumeCurrency(TriggeRunGameManager.Instance.GemCurrency,
                            TriggeRunGameManager.Instance.PresetCosts[obj.CurrentIndex]))
                    {
                        TriggeRunGameManager.Instance.CurrentPlayerData.presets.Add(TriggeRunGameManager.Instance.CurrentPlayerData.presets.Last().TrickDeepClone());
                        int newIndex = TriggeRunGameManager.Instance.CurrentPlayerData.presets.Count - 1;
                        TriggeRunGameManager.Instance.CurrentPlayerData.selectedPresetIndex = newIndex;
                        
                        TriggeRunGameManager.Instance.SetAndFetchPublicClientStatsVoid(new Dictionary<string, string>
                        {
                            [nameof(TriggeRunPlayerData.selectedPresetIndex)] = newIndex.ToString(),
                            [nameof(TriggeRunPlayerData.presets)] = TriggeRunGameManager.Instance.CurrentPlayerData.presets.SerializeToJson(false, true)
                        }, true, () =>
                        {
                            RefreshView();
                        });
                    }
                    else
                    {
                        ModalPopupMenu.ShowOkModal("Insufficient Funds.", $"Not enough {"Gems".ToStringColor(Color.yellow)} to purchase this.", "Ok",
                            () =>
                            {
                                
                            });
                    }
                }, () =>
                {
                    
                });
        }
    }

    public void UpdateSelectEquipped()
    {
        if (_currentSelectedItemInventoryEntry != null)
        {
            _currentSelectedItemInventoryEntry.RefreshView();
        }
    }
}