using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Beamable;
using Beamable.Common.Api.Stats;
using Beamable.Common.Content;
using Beamable.Common.Inventory;
using Beamable.Player;
using Beamable.Server.Clients;
using BeauRoutine;
using IEdgeGames;
using Newtonsoft.Json;
using TMPro;
using TrickCore;
using UnityEngine;
using UnityEngine.UI;

public class MainMenu : UIMenu
{
    [Header("Game Modes")]
    public Button LobbyRoyaleButton;
    public Button SuperBlastButton;
    public Button DeathmatchButton;
    public Button RankedButton;
    
    [Header("Other Buttons")]
    public Button ShopButton;
    
    public Button ProfileButton;
    public Button RoomButton;
    public Button FriendsButton;
    public Button SettingsButton;

    [Header("Header")] 
    public TextMeshProUGUI PlayerNameText;
    public TextMeshProUGUI PlayerLevelText;
    public TextMeshProUGUI CoinCurrencyText;
    public TextMeshProUGUI GemCurrencyText;
    
    public CurrencyRef CoinCurrency;
    public CurrencyRef GemCurrency;
    
    [Header("Avatar")]
    public bool AvatarPlayWalkAnimation;
    public Transform AvatarRoot;
    
    [Header("Presets")]
    public InventoryPresetSlotEntry PresetSlotEntryPrefab;
    public RectTransform PresetSlotEntryContentTransform;
    
    [Header("Others")]
    private GameObject _lastCharacter;

    private List<InventoryPresetSlotEntry> _presets = new List<InventoryPresetSlotEntry>();

    protected override void AddressablesAwake()
    {
        base.AddressablesAwake();
        
        ShopButton.onClick.AddListener(() => UIManager.Instance.ToggleMenu<ShopMenu>(menu => Hide(), null));
        SettingsButton.onClick.AddListener(() => UIManager.Instance.ToggleMenu<SettingsMenu>(menu => Hide(), null));
        ProfileButton.onClick.AddListener(() => UIManager.Instance.ToggleMenu<InventoryMenu>(menu => Hide(), null));

        LobbyRoyaleButton.onClick.AddListener(StartLobbyRoyale);
        SuperBlastButton.onClick.AddListener(StartSuperBlast);
        DeathmatchButton.onClick.AddListener(StartDeathmatch);
        RankedButton.onClick.AddListener(StartRanked);
    }

    private void StartRanked()
    {
        ModalPopupMenu.ShowOkModal("Error!", "Ranked".ToStringColor(Color.yellow) + " not implemented yet.", "Ok", null);
    }

    private void StartDeathmatch()
    {
        UIManager.Instance.GetMenu<FindMatchMenu>().Show();
    }

    private void StartSuperBlast()
    {
        ModalPopupMenu.ShowOkModal("Error!", "Superblast".ToStringColor(Color.yellow) + " not implemented yet.", "Ok", null);
    }

    private void StartLobbyRoyale()
    {
        ModalPopupMenu.ShowOkModal("Error!", "Lobby Royale".ToStringColor(Color.yellow) + " not implemented yet.", "Ok", null);
    }

    public override UIMenu Show()
    {
        base.Show();

        if (string.IsNullOrEmpty(TriggeRunGameManager.Instance.CurrentPlayerData.alias))
        {
            UIManager.Instance.GetMenu<InputPopupMenu>().HideOnResponseClicked = false;
            InputPopupMenu.ShowInputModal("Enter your name", "", "SET NAME", (s) =>
            {
                InputPopupMenu.EnableButton(1, false);
                bool? nameSetResponse = null;
                UIManager.Instance.GetMenu<LoadingMenu>().WaitFor("Submitting name...", () =>
                {
                    if (nameSetResponse.GetValueOrDefault())
                    {
                        UIManager.Instance.GetMenu<InputPopupMenu>().Hide();
                    }
                    else
                    {
                        InputPopupMenu.EnableButton(1, true);
                    }
                }, () =>
                {
                    return nameSetResponse != null;
                }).Show();

                RequestSetName(b =>
                {
                    nameSetResponse = b;
                });
                
                void RequestSetName(Action<bool> succeedCallback)
                {
                    var serviceClient = new TRMicroServiceClient();
                    serviceClient.IsUsernameAvailable(s).Then(b =>
                    {
                        Debug.Log($"Username '{s}' is available={b.Available}!");
                        if (b.Available)
                        {
                            serviceClient.ClaimUsername(TriggeRunGameManager.Instance.CurrentUser.id, s).Then(b1 =>
                            {
                                Debug.Log($"[CLAIM] Username '{s}' is claimed={b1}!");
                                /*TriggeRunGameManager.Instance.CurrentContext.Stats.Refresh().Then(unit =>
                                {
                                    TriggeRunGameManager.Instance.FetchPublicClientStatsVoid(() =>
                                    {
                                        succeedCallback?.Invoke(true);
                                    });                            
                                });*/
                                TriggeRunGameManager.Instance.SetAndFetchPublicClientStatsVoid(new Dictionary<string, string>()
                                {
                                    {"alias", s}
                                }, true, LoadStats);
                                succeedCallback?.Invoke(true);                             
                            }).Error(exception =>
                            {
                                ModalPopupMenu.ShowError(exception);
                                succeedCallback?.Invoke(false);
                            });
                        }
                        else
                        {
                            ModalPopupMenu.ShowError(b.Message);
                            succeedCallback?.Invoke(false);
                        }
                    }).Error(exception =>
                    {
                        ModalPopupMenu.ShowError(exception);
                        succeedCallback?.Invoke(false);
                    });;
                }
                
                
            }, s =>
            {
                IEnumerator UpdateDescription()
                {
                    yield break;
                }
                return UpdateDescription();
            });
        }


        RefreshView();

        return this;
    }

    private void RefreshView()
    {
        TriggeRunGameManager.Instance.MaxNumPresets.PoolGetOrInstantiate(ref _presets, (entry, i) =>
        {
            var preset = i >= 0 && i < TriggeRunGameManager.Instance.CurrentPlayerData.presets.Count
                ? TriggeRunGameManager.Instance.CurrentPlayerData.presets[i]
                : null;
            entry.SetData(preset, i, OnSelectPreset);
        }, i => Instantiate(PresetSlotEntryPrefab, PresetSlotEntryContentTransform));
        
        LoadStats();
        
        // Draw our avatar
        var currentCharacter = TriggeRunGameManager.Instance.GetCurrentCharacter();
        if (currentCharacter)
        {
            if (_lastCharacter != null) Destroy(_lastCharacter);
            (_lastCharacter = Instantiate(currentCharacter.uiPrefabDirect, AvatarRoot)).SetLayerRecursively("UI");

            if (AvatarPlayWalkAnimation)
            {
                var animator = _lastCharacter.GetComponent<Animator>();
                animator.Play("Walk");
            }
        }
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

    private async void LoadStats()
    {
        var stats = TriggeRunGameManager.Instance.CurrentPlayerData;
        PlayerNameText.text = stats.alias;
        PlayerLevelText.text = stats.level.ToString();
    }

    private void StatsOnOnDataUpdated(SerializableDictionaryStringToSomething<PlayerStat> obj)
    {
        foreach (KeyValuePair<string,PlayerStat> pair in obj)
        {
            if (pair.Key == "alias")
            {
                PlayerNameText.text = pair.Value.Value;
            }
            
            if (pair.Key == "level")
            {
                PlayerLevelText.text = pair.Value.Value;
            }
        }
    }
}

[JsonObject, Serializable]
public class TriggeRunPlayerData
{
    [JsonProperty(PropertyName = "alias")]
    public string alias;
    [JsonProperty(PropertyName = "level")]
    public int level;
    [JsonProperty(PropertyName = "xp")]
    public int xp;

    [JsonProperty(PropertyName = "heroes")]
    public List<TriggeRunHeroData> heroes = new List<TriggeRunHeroData>();
    
    [JsonProperty(PropertyName = "items")]
    public List<TriggeRunItemData> items = new List<TriggeRunItemData>();

    [JsonProperty(PropertyName = "selectedPresetIndex")]
    public int selectedPresetIndex;
    
    [JsonProperty(PropertyName = "presets")]
    public List<TriggeRunPresetData> presets = new List<TriggeRunPresetData>();

    public TriggeRunPresetData GetPreset()
    {
        if (selectedPresetIndex >= 0 && selectedPresetIndex < presets.Count) return presets[selectedPresetIndex];
        return null;
    }

    public async Task Validate()
    {
        var dirty = new Dictionary<string, string>();

        // update our default values if anything is on default
        if (level == 0)
        {
            dirty[nameof(level)] = "1";
            dirty[nameof(xp)] = "0";
        }

        // give the default heroes
        if (heroes == null)
        {
            heroes = TriggeRunGameManager.Instance.GetDefaultHeroes();
            dirty[nameof(heroes)] = heroes.SerializeToJson(false, true);
        }
        else
        {
            // Give the default heroes that are missing, we might add new default heroes later, sso we append the default hero into the current list
            var defaultHeroes = TriggeRunGameManager.Instance.GetDefaultHeroes();
            var notAddedHeroes = defaultHeroes.Where(data => heroes.All(heroData => heroData != data))
                .ToList();
            if (notAddedHeroes.Count > 0)
            {
                heroes.AddRange(notAddedHeroes);
                dirty[nameof(heroes)] = heroes.SerializeToJson(false, true);
            }
        }

        // give default items
        if (items == null)
        {
            items = TriggeRunGameManager.Instance.GetDefaultItems();
            dirty[nameof(items)] = items.SerializeToJson(false, true);
        }
        else
        {
            // Give the default items that are missing, we might add new default items later, sso we append the default items into the current list
            var defaultItems = TriggeRunGameManager.Instance.GetDefaultItems();
            var notAddedItems = defaultItems.Where(data => items.All(itemData => itemData != data))
                .ToList();
            if (notAddedItems.Count > 0)
            {
                items.AddRange(notAddedItems);
                dirty[nameof(items)] = items.SerializeToJson(false, true);
            }
        }

        // generate preset, player starts with always one preset
        if (presets == null || presets.Count == 0)
        {
            presets = new List<TriggeRunPresetData>()
            {
                TriggeRunPresetData.CreateNew(heroes.Select(data => data.HeroId).FirstOrDefault(),
                    items
                        .Select(data => data.GetItem())
                        .GroupBy(definition => definition.SlotCategory)
                        .SelectMany(grouping =>
                            grouping.Take(TriggeRunGameManager.Instance.ItemSlotCategoryCarryAmount.TryGetValue(grouping.Key, out var limit)
                                ? limit
                                : 1))
                        .Select(definition => definition.id)
                        .Distinct()
                        .ToList()
                )
            };
            dirty[nameof(presets)] = presets.SerializeToJson(false, true);
        }
        else
        {
            bool isDirty = false;
            presets.ForEach(data =>
            {
                if (data.ValidateDirtyCheck()) isDirty = true;
            });
            if (isDirty) dirty[nameof(presets)] = presets.SerializeToJson(false, true);
        }

        // if any dirty, we save
        if (dirty.Count > 0) await TriggeRunGameManager.Instance.SetAndFetchPublicClientStats(dirty, true);
    }
}

[JsonObject, Serializable]
public class TriggeRunHeroData : IEquatable<TriggeRunHeroData>
{
    public int HeroId;
    public SkinOwnedEnum OwnedSkins;
    public DateTime UnlockDate;
    
    
    private CharacterDefinition _cached;

    public CharacterDefinition GetHero()
    {
        return _cached ??= TriggeRunGameManager.Instance.CharacterDefinitions.Find(definition => definition.id == HeroId);
    }

    public bool Equals(TriggeRunHeroData other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return HeroId == other.HeroId;
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((TriggeRunHeroData)obj);
    }

    public override int GetHashCode()
    {
        return HeroId;
    }

    public static bool operator ==(TriggeRunHeroData left, TriggeRunHeroData right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(TriggeRunHeroData left, TriggeRunHeroData right)
    {
        return !Equals(left, right);
    }
}

[Flags]
public enum SkinOwnedEnum
{
    Default = 0,
    One = 1 << 0,
    Two = 1 << 1,
    Three = 1 << 2,
    Four = 1 << 3,
    Five = 1 << 4,
    Six = 1 << 5,
    Seven = 1 << 6,
    Eight = 1 << 7,
    Nine = 1 << 8,
    Ten = 1 << 9,
}

[JsonObject, Serializable]
public class TriggeRunItemData : IEquatable<TriggeRunItemData>
{
    public int ItemId;
    public SkinOwnedEnum OwnedSkins;
    public DateTime UnlockDate;
    
    
    private ItemDefinition _cached;

    public ItemDefinition GetItem()
    {
        return _cached ??= TriggeRunGameManager.Instance.ItemDefinitions.Find(definition => definition.id == ItemId);
    }

    public bool Equals(TriggeRunItemData other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return ItemId == other.ItemId;
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((TriggeRunItemData)obj);
    }

    public override int GetHashCode()
    {
        return ItemId;
    }

    public static bool operator ==(TriggeRunItemData left, TriggeRunItemData right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(TriggeRunItemData left, TriggeRunItemData right)
    {
        return !Equals(left, right);
    }
}

[JsonObject, Serializable]
public class TriggeRunPresetData
{
    public int HeroId;
    public int HeroSkinIndex;
    public List<int> ItemIds;
    public List<int> ItemSkinIndices;

    public static TriggeRunPresetData CreateNew(int heroId, List<int> items)
    {
        return new TriggeRunPresetData()
        {
            HeroId = heroId,
            ItemIds = items
        };
    }

    /// <summary>
    /// If returns true, it's dirty so it requires an update
    /// </summary>
    /// <returns></returns>
    public bool ValidateDirtyCheck()
    {
        // check if things still exists
        
        // check for duplicate items etc

        if (ItemSkinIndices == null)
        {
            ItemSkinIndices = new List<int>().ListPadding(ItemIds.Count).ToList();
            return true;
        }

        if (ItemSkinIndices.Count != ItemIds.Count)
        {
            ItemSkinIndices = ItemSkinIndices.ListPadding(ItemIds.Count).ToList();
            return true;
        }

        return false;
    }
}