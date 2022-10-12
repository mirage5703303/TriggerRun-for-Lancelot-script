using System;
using System.Collections.Generic;
using System.Linq;
using Beamable.Common.Inventory;
using IEdgeGames;
using TMPro;
using TrickCore;
using UnityEngine;
using UnityEngine.UI;
using HighlightState = TrickCore.HighlightState;

/// <summary>
/// The panel to show an Item or an Hero
/// </summary>
public class SlotInfoPanel : MonoBehaviour
{
    public AttributeStatEntry AttributeStatEntryPrefab;
    public RectTransform AttributeStatEntryContentTransform;

    public SkinSlotEntry SkinSlotEntryPrefab;
    public RectTransform SkinSlotEntryContentTransform;
    public RectTransform SkinViewRoot;
    
    public Button CoinPurchaseButton;
    public Button GemPurchaseButton;
    public Button EquipButton;
    public TextMeshProUGUI CoinValueText;
    public TextMeshProUGUI GemValueText;
    public TextMeshProUGUI EquipText;

    public Image ItemLevelFlairImage;
    public TextMeshProUGUI NameText;
    
    public string EquipString = "Equip";
    public string UnequipString = "Unequip";
    
    private List<AttributeStatEntry> _attributes;
    private List<SkinSlotEntry> _skinSlots;
    private SkinSlotEntry _targetSkinSlot;

    public ItemDefinition Item { get; set; }
    public CharacterDefinition Hero { get; set; }

    public ShopItemEntry SelectedItemEntry { get; set; }
    public InventorySlotEntry SelectedInventorySlotEntry { get; set; }

    private void Awake()
    {
        int GetCoinCost()
        {
            if (Item != null)
            {
                if (Item.HasOwned() && _targetSkinSlot != null && !_targetSkinSlot.HasSkin)
                {
                    return _targetSkinSlot.CoinCosts;
                }
                
                return Item.CoinCosts;
            }
            if (Hero.HasOwned() && _targetSkinSlot != null && !_targetSkinSlot.HasSkin)
            {
                return _targetSkinSlot.CoinCosts;
            }
            return Hero.CoinCosts;
        }
        
        int GetGemCost()
        {
            if (Item != null)
            {
                if (Item.HasOwned() && _targetSkinSlot != null && !_targetSkinSlot.HasSkin)
                {
                    return _targetSkinSlot.GemCosts;
                }
                
                return Item.GemCosts;
            }
            if (Hero.HasOwned() && _targetSkinSlot != null && !_targetSkinSlot.HasSkin)
            {
                return _targetSkinSlot.GemCosts;
            }
            return Hero.GemCosts;
        }

        CoinPurchaseButton.onClick.AddListener(() => PurchaseWith(TriggeRunGameManager.Instance.CoinCurrency, "Coins", GetCoinCost()));
        GemPurchaseButton.onClick.AddListener(() => PurchaseWith(TriggeRunGameManager.Instance.GemCurrency, "Gems", GetGemCost()));
        EquipButton.onClick.AddListener(TryEquip);
    }

    public void TryEquip()
    {
        EquipButton.interactable = false;
        if (Item != null)
        {
            // equip the item
            var activePreset = TriggeRunGameManager.Instance.CurrentPlayerData.GetPreset();

            if (activePreset.ItemIds.IndexOf(Item.id) is {} itemIndex && itemIndex != -1)
            {
                // unequip
                activePreset.ItemIds.RemoveAt(itemIndex);
                activePreset.ItemSkinIndices.RemoveAt(itemIndex);
                
                // make sure the skin index has the same count as the item ids
                activePreset.ItemSkinIndices = activePreset.ItemSkinIndices.ListPadding(activePreset.ItemIds.Count).ToList();
            }
            else
            {
                List<(int id, int skinIndex)> idAndSkinIndex = UIManager.Instance.GetMenu<InventoryMenu>()
                    .InventorySlotEntries.Where(entry => !entry.IsHeroSlot)
                    .Select(entry =>
                    {
                        // we basically swap with what we had in the inventory
                        if (entry == SelectedInventorySlotEntry)
                        {
                            return (Item.id, _targetSkinSlot != null ? _targetSkinSlot.SkinIndex : 0);
                        }

                        return (entry.ItemData?.ItemId ?? 0, entry.SkinIndex);
                    })
                    .Where(i => i.Item1 != 0)
                    .ToList();

                activePreset.ItemIds = idAndSkinIndex.Select(tuple => tuple.id).ToList();
                activePreset.ItemSkinIndices = idAndSkinIndex.Select(tuple => tuple.skinIndex).ToList();
            }

            TriggeRunGameManager.Instance.SetAndFetchPublicClientStatsVoid(
                new Dictionary<string, string>()
                {
                    {
                        nameof(TriggeRunPlayerData.presets),
                        TriggeRunGameManager.Instance.CurrentPlayerData.presets.SerializeToJson(false, true)
                    }
                }, true, () =>
                {
                    EquipButton.interactable = true;
                    UIManager.Instance.GetMenu<InventoryMenu>().RefreshView();
                });
        }
        else if (Hero != null)
        {
            // equip the hero
            var activePreset = TriggeRunGameManager.Instance.CurrentPlayerData.GetPreset();
            activePreset.HeroId = Hero.id;
            TriggeRunGameManager.Instance.SetAndFetchPublicClientStatsVoid(new Dictionary<string, string>() { { nameof(TriggeRunPlayerData.presets), TriggeRunGameManager.Instance.CurrentPlayerData.presets.SerializeToJson(false, true) } }, true, () =>
            {
                EquipButton.interactable = true;
                UIManager.Instance.GetMenu<InventoryMenu>().RefreshView();
            });
        }
    }

    private void PurchaseWith(CurrencyRef currencyRef, string currencyName, int cost)
    {
        CoinPurchaseButton.interactable = false;
        GemPurchaseButton.interactable = false;
        if (TriggeRunGameManager.Instance.TryConsumeCurrency(currencyRef, cost))
        {
            if (Item != null)
            {
                if (!Item.HasOwned())
                {
                    TriggeRunGameManager.Instance.CurrentPlayerData.items.Add(new TriggeRunItemData()
                    {
                        ItemId = Item.id,
                        OwnedSkins = SkinOwnedEnum.Default,
                        UnlockDate = DateTime.UtcNow
                    });

                    int purchaseId = Item.id;
                    // do the purchase
                    TriggeRunGameManager.Instance.SetAndFetchPublicClientStatsVoid(new Dictionary<string, string>
                    {
                        [nameof(TriggeRunPlayerData.items)] = TriggeRunGameManager.Instance.CurrentPlayerData.items.SerializeToJson(false, true)
                    }, true, () =>
                    {
                        CoinPurchaseButton.interactable = true;
                        GemPurchaseButton.interactable = true;
                        UIManager.Instance.GetMenu<InventoryMenu>().RefreshView();
                        UIManager.Instance.GetMenu<InventoryMenu>().SelectItemById(purchaseId);
                        TryEquip();
                    });
                }
                else if (_targetSkinSlot != null)
                {
                    Debug.Log($"Handle skin purchase hero > {_targetSkinSlot.SkinIndex}");

                    var itemData = TriggeRunGameManager.Instance.CurrentPlayerData.items.Find(data => data.ItemId == Item.id);
                    itemData.OwnedSkins |= (SkinOwnedEnum)(1 << _targetSkinSlot.SkinIndex);
                    
                    var preset = TriggeRunGameManager.Instance.CurrentPlayerData.GetPreset();
                    preset.ItemSkinIndices = preset.ItemSkinIndices.ListPadding(preset.ItemIds.Count).ToList();
                    if (_targetSkinSlot != null && _targetSkinSlot.Item != null)
                    {
                        int itemIndex = preset.ItemIds.IndexOf(Item.id);
                        if (itemIndex != -1) preset.ItemSkinIndices[itemIndex] = _targetSkinSlot.SkinIndex;
                    }

                    int purchaseId = Item.id;
                    // do the purchase
                    TriggeRunGameManager.Instance.SetAndFetchPublicClientStatsVoid(new Dictionary<string, string>
                    {
                        [nameof(TriggeRunPlayerData.items)] = TriggeRunGameManager.Instance.CurrentPlayerData.items.SerializeToJson(false, true),
                        [nameof(TriggeRunPlayerData.presets)] = TriggeRunGameManager.Instance.CurrentPlayerData.presets.SerializeToJson(false, true),
                    }, true, () =>
                    {
                        CoinPurchaseButton.interactable = true;
                        GemPurchaseButton.interactable = true;
                        UIManager.Instance.GetMenu<InventoryMenu>().RefreshView();
                    });
                    
                    CoinPurchaseButton.interactable = true;
                    GemPurchaseButton.interactable = true;
                }
            }
            else if (Hero != null)
            {
                if (!Hero.HasOwned())
                {
                    TriggeRunGameManager.Instance.CurrentPlayerData.heroes.Add(new TriggeRunHeroData()
                    {
                        HeroId = Hero.id,
                        UnlockDate = DateTime.UtcNow,
                    });

                    int purchaseId = Hero.id;
                    // do the purchase
                    TriggeRunGameManager.Instance.SetAndFetchPublicClientStatsVoid(new Dictionary<string, string>
                    {
                        [nameof(TriggeRunPlayerData.heroes)] = TriggeRunGameManager.Instance.CurrentPlayerData.heroes.SerializeToJson(false, true)
                    }, true, () =>
                    {
                        CoinPurchaseButton.interactable = true;
                        GemPurchaseButton.interactable = true;
                        UIManager.Instance.GetMenu<InventoryMenu>().RefreshView();
                        UIManager.Instance.GetMenu<InventoryMenu>().SelectHeroById(purchaseId);
                        TryEquip();
                    });
                }
                else if (_targetSkinSlot != null)
                {
                    Debug.Log($"Handle skin purchase item > {_targetSkinSlot.SkinIndex}");
                    
                    var itemData = TriggeRunGameManager.Instance.CurrentPlayerData.heroes.Find(data => data.HeroId == Hero.id);
                    itemData.OwnedSkins |= (SkinOwnedEnum)(1 << _targetSkinSlot.SkinIndex);

                    var preset = TriggeRunGameManager.Instance.CurrentPlayerData.GetPreset();
                    preset.HeroSkinIndex = _targetSkinSlot.SkinIndex;
                    
                    int purchaseId = Hero.id;
                    // do the purchase
                    TriggeRunGameManager.Instance.SetAndFetchPublicClientStatsVoid(new Dictionary<string, string>
                    {
                        [nameof(TriggeRunPlayerData.heroes)] = TriggeRunGameManager.Instance.CurrentPlayerData.heroes.SerializeToJson(false, true),
                        [nameof(TriggeRunPlayerData.presets)] = TriggeRunGameManager.Instance.CurrentPlayerData.presets.SerializeToJson(false, true),
                    }, true, () =>
                    {
                        CoinPurchaseButton.interactable = true;
                        GemPurchaseButton.interactable = true;
                        UIManager.Instance.GetMenu<InventoryMenu>().RefreshView();
                    });
                    
                    CoinPurchaseButton.interactable = true;
                    GemPurchaseButton.interactable = true;
                }
            }
        }
        else
        {
            ModalPopupMenu.ShowOkModal("Insufficient Funds.", $"Not enough {currencyName.ToStringColor(Color.yellow)} to purchase this.", "Ok",
                () =>
                {
                    CoinPurchaseButton.interactable = true;
                    GemPurchaseButton.interactable = true;
                });
        }
    }

    public void SetHero(CharacterDefinition hero)
    {
        if (hero == null)
        {
            Item = null;
            Hero = null;
            gameObject.SetActive(false);
            return;
        }

        if (Hero == hero) return;
        
        ItemLevelFlairImage.gameObject.SetActive(false);
        NameText.text = hero.name;
        Hero = hero;
        Item = null;
        SelectedItemEntry = null;
        SelectedInventorySlotEntry = null;
        
        hero.GetClassAttributes().PoolGetOrInstantiate(ref _attributes, (entry, pair, arg3) =>
        {
            entry.AttributeNameText.text = pair.Key;
            entry.AttributeValueText.text = pair.Value.GetString();
            entry.FillImage.fillAmount = pair.Value.GetAlpha();
        }, (pair, i) => Instantiate(AttributeStatEntryPrefab, AttributeStatEntryContentTransform));

        if (hero.HasOwned())
        {
            SkinViewRoot.gameObject.SetActive(true);
            hero.GetAllSkins().PoolGetOrInstantiate(ref _skinSlots, (entry, data, arg3) =>
            {
                entry.SetHero(hero, data, arg3, OnHeroSkinClicked);
            }, (data, i) => Instantiate(SkinSlotEntryPrefab, SkinSlotEntryContentTransform));

            _targetSkinSlot = null;
            
            var heroData = TriggeRunGameManager.Instance.CurrentPlayerData?.heroes?.Find(data => data.HeroId == hero.id);
            if (heroData != null)
            {
                var currentPreset = TriggeRunGameManager.Instance.CurrentPlayerData.GetPreset();
                if (currentPreset.HeroId == hero.id)
                {
                    var firstSkin = _skinSlots.FirstOrDefault(entry => heroData.OwnedSkins == SkinOwnedEnum.Default || heroData.OwnedSkins.HasFlag((SkinOwnedEnum)(1 << currentPreset.HeroSkinIndex)));
                    if (firstSkin != null) OnHeroSkinClicked(firstSkin);
                }
                else
                {
                    int index = 0;
                    var firstSkin = _skinSlots.FirstOrDefault(entry => heroData.OwnedSkins == SkinOwnedEnum.Default || heroData.OwnedSkins.HasFlag((SkinOwnedEnum)(1 << index++)));
                    if (firstSkin != null) OnHeroSkinClicked(firstSkin);
                }
            }
            
            CoinPurchaseButton.gameObject.SetActive(false);
            GemPurchaseButton.gameObject.SetActive(false);
            EquipButton.gameObject.SetActive(false);
            EquipText.text = EquipString;
        }
        else
        {
            SkinViewRoot.gameObject.SetActive(false);
            
            EquipButton.gameObject.SetActive(false);

            CoinPurchaseButton.gameObject.SetActive(hero.CoinCosts >= 0);
            CoinValueText.text = hero.CoinCosts == 0 ? "FREE" : hero.CoinCosts.ToString("N0");
        
            GemPurchaseButton.gameObject.SetActive(hero.GemCosts >= 0);
            GemValueText.text = hero.GemCosts == 0 ? "FREE" : hero.GemCosts.ToString("N0");
        }
        gameObject.SetActive(true);
    }

    private void OnHeroSkinClicked(SkinSlotEntry obj)
    {
        _skinSlots.ForEach(entry => TrickVisualHelper.SetHighlighted(entry, null, entry == obj ? HighlightState.AlwaysOn : HighlightState.Off));

        _targetSkinSlot = obj;
        
        CoinPurchaseButton.gameObject.SetActive(!obj.HasSkin && obj.CoinCosts >= 0);
        CoinValueText.text = obj.CoinCosts == 0 ? "FREE" : obj.CoinCosts.ToString("N0");
        
        GemPurchaseButton.gameObject.SetActive(!obj.HasSkin && obj.GemCosts >= 0);
        GemValueText.text = obj.GemCosts == 0 ? "FREE" : obj.GemCosts.ToString("N0");

        if (obj.HasSkin)
        {
            var preset = TriggeRunGameManager.Instance.CurrentPlayerData.GetPreset();
            if (preset.HeroId == Hero.id && preset.HeroSkinIndex != _targetSkinSlot.SkinIndex)
            {
                preset.HeroSkinIndex = _targetSkinSlot.SkinIndex;
                TriggeRunGameManager.Instance.SetAndFetchPublicClientStatsVoid(new Dictionary<string, string>()
                {
                    {
                        nameof(TriggeRunPlayerData.presets),
                        TriggeRunGameManager.Instance.CurrentPlayerData.presets.SerializeToJson(false, true)
                    }
                }, false);
            }
            
            EquipButton.gameObject.SetActive(!obj.Hero.HasEquipped());
        }
        else
        {
            EquipButton.gameObject.SetActive(false);
        }
    }

    private void OnItemSkinClicked(SkinSlotEntry obj)
    {
        _skinSlots.ForEach(entry => TrickVisualHelper.SetHighlighted(entry, null, entry == obj ? HighlightState.AlwaysOn : HighlightState.Off));
        
        _targetSkinSlot = obj;
        
        CoinPurchaseButton.gameObject.SetActive(!obj.HasSkin && obj.CoinCosts >= 0);
        CoinValueText.text = obj.CoinCosts == 0 ? "FREE" : obj.CoinCosts.ToString("N0");
        
        GemPurchaseButton.gameObject.SetActive(!obj.HasSkin && obj.GemCosts >= 0);
        GemValueText.text = obj.GemCosts == 0 ? "FREE" : obj.GemCosts.ToString("N0");

        if (obj.HasSkin)
        {
            var preset = TriggeRunGameManager.Instance.CurrentPlayerData.GetPreset();
            if (preset.ItemIds.IndexOf(Item.id) is { } itemIndex && itemIndex >= 0 &&
                itemIndex < preset.ItemSkinIndices.Count &&
                preset.ItemSkinIndices[itemIndex] != _targetSkinSlot.SkinIndex)
            {
                preset.ItemSkinIndices[itemIndex] = _targetSkinSlot.SkinIndex;
                TriggeRunGameManager.Instance.SetAndFetchPublicClientStatsVoid(new Dictionary<string, string>()
                {
                    {
                        nameof(TriggeRunPlayerData.presets),
                        TriggeRunGameManager.Instance.CurrentPlayerData.presets.SerializeToJson(false, true)
                    }
                }, false);

                UIManager.Instance.GetMenu<InventoryMenu>().UpdateSelectEquipped();
            }
            
            EquipButton.gameObject.SetActive(true);
        }
        else
        {
            EquipButton.gameObject.SetActive(false);
        }
    }


    public void SetItem(ItemDefinition item, ShopItemEntry shopItemEntry, InventorySlotEntry inventorySlotEntry)
    {
        if (item == null)
        {
            Item = null;
            Hero = null;
            gameObject.SetActive(false);
            return;
        }
        if (Item == item) return;
        ItemLevelFlairImage.gameObject.SetActive(true);
        NameText.text = item.Name;
        Item = item;
        SelectedItemEntry = shopItemEntry;
        SelectedInventorySlotEntry = inventorySlotEntry;
        Hero = null;
        
        item.GetStatsAttributes().PoolGetOrInstantiate(ref _attributes, (entry, pair, arg3) =>
        {
            entry.AttributeNameText.text = pair.Key;
            entry.AttributeValueText.text = pair.Value.GetString();
            entry.FillImage.fillAmount = pair.Value.GetAlpha();
        }, (pair, i) => Instantiate(AttributeStatEntryPrefab, AttributeStatEntryContentTransform));
        
        if (item.HasOwned())
        {
            SkinViewRoot.gameObject.SetActive(true);
            item.GetAllSkins().PoolGetOrInstantiate(ref _skinSlots, (entry, data, arg3) =>
            {
                entry.SetItem(item, data, arg3, OnItemSkinClicked);
            }, (data, i) => Instantiate(SkinSlotEntryPrefab, SkinSlotEntryContentTransform));

            _targetSkinSlot = null;
            
            var itemData = TriggeRunGameManager.Instance.CurrentPlayerData?.items?.Find(data => data.ItemId == item.id);
            if (itemData != null)
            {
                var currentPreset = TriggeRunGameManager.Instance.CurrentPlayerData.GetPreset();
                if (currentPreset.ItemIds.IndexOf(item.id) is {} itemIndex && itemIndex != -1)
                {
                    int index = 0;
                    var firstSkin = _skinSlots.FirstOrDefault(entry => itemData.OwnedSkins == SkinOwnedEnum.Default || index++ == currentPreset.ItemSkinIndices[itemIndex]);
                    if (firstSkin != null) OnItemSkinClicked(firstSkin);
                }
                else
                {
                    int index = 0;
                    var firstSkin = _skinSlots.FirstOrDefault(entry => itemData.OwnedSkins == SkinOwnedEnum.Default || itemData.OwnedSkins.HasFlag((SkinOwnedEnum)(1 << index++)));
                    if (firstSkin != null) OnItemSkinClicked(firstSkin);
                }
               
            }
            
            CoinPurchaseButton.gameObject.SetActive(false);
            GemPurchaseButton.gameObject.SetActive(false);
            EquipButton.gameObject.SetActive(true);

            EquipText.text = item.HasEquipped() ? UnequipString : EquipString;
        }
        else
        {
            SkinViewRoot.gameObject.SetActive(false);
            
            EquipButton.gameObject.SetActive(false);
            
            CoinPurchaseButton.gameObject.SetActive(item.CoinCosts >= 0);
            CoinValueText.text = item.CoinCosts == 0 ? "FREE" : item.CoinCosts.ToString("N0");
        
            GemPurchaseButton.gameObject.SetActive(item.GemCosts >= 0);
            GemValueText.text = item.GemCosts == 0 ? "FREE" : item.GemCosts.ToString("N0");
        }
        gameObject.SetActive(true);
    }


    public void RefreshView()
    {
        if (Item != null)
        {
            var item = Item;
            Item = null;
            SetItem(item, SelectedItemEntry, SelectedInventorySlotEntry);
        }
        else if (Hero != null)
        {
            var hero = Hero;
            Hero = null;
            SetHero(hero);
        }
    }
}