using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Beamable;
using Beamable.Common.Api.Auth;
using Beamable.Common.Api.Inventory;
using Beamable.Common.Inventory;
using BeauRoutine;
using IEdgeGames;
using Newtonsoft.Json.Linq;
using Opsive.UltimateCharacterController.Inventory;
using Opsive.UltimateCharacterController.Items.Actions;
using Sirenix.OdinInspector;
using TMPro;
using TrickCore;
using UnityEngine;
using UnityEngine.Events;
using Item = Opsive.UltimateCharacterController.Items.Item;

/// <summary>
/// A persistent game manager, controlling some of the flows for the game
/// </summary>
public class TriggeRunGameManager : MonoSingleton<TriggeRunGameManager>
{
    /// <summary>
    /// Just a stub function in order to debug
    /// </summary>
    [Button]
    void DebugBreakIn()
    {
        Debug.Log("Set a breakpoint here!");
    }

    public CurrencyRef GemCurrency;
    public CurrencyRef CoinCurrency;
    
    public List<CurrencyRef> Currencies = new List<CurrencyRef>();
    public TweenSettings CurrencyTextLerpSettings;

    [ListDrawerSettings(ListElementLabelName = "ListElementLabelName"),Searchable, InlineEditor()]
    public List<CharacterDefinition> CharacterDefinitions = new List<CharacterDefinition>();
    [ListDrawerSettings(ListElementLabelName = "ListElementLabelName"),Searchable, InlineEditor()]
    public List<ItemDefinition> ItemDefinitions = new List<ItemDefinition>();

    public Dictionary<ItemSlotCategory, int> ItemSlotCategoryCarryAmount;
    public Sprite CurrencyCoinSprite;
    public Sprite CurrencyGemSprite;

    public TRCharacter Character;
    
    public ItemStatsData MaxItemStats;
    public HeroClassStatsData MaxClassStats;
    public Dictionary<HeroClassType, HeroClassStatsData> ClassStats;
    
    public TriggeRunPresetData AIPreset;
    public int MaxNumPresets = 5;
    public List<int> PresetCosts = new List<int>();

    /// <summary>
    /// The current logged in user
    /// </summary>
    public User CurrentUser { get; private set; }

    public BeamContext CurrentContext => BeamContext.Default;
    public TriggeRunPlayerData CurrentPlayerData { get; private set; }

    public UnityEvent<(CurrencyRef Currency, long PreviousValue, long NewValue)> CurrencyUpdateEvent { get; set; } =
        new UnityEvent<(CurrencyRef Currency, long PreviousValue, long NewValue)>();
    
    private Dictionary<string, long> _currencyAmount { get; } = new Dictionary<string, long>();

    protected override void Initialize()
    {
        base.Initialize();
    }

    protected override void Start()
    {
        base.Start();
        
        // Whenever we start the game, we start with a splash screen
        UIManager.Instance.ShowOnly<SplashMenu>();
    }

    public void Logout()
    {
        CurrentUser = default;
    }
    public void SetBeamableContext(BeamContext context, Action<string> contextReadyCallback)
    {
        var ctx = context;
        CurrentContext.OnReady.Then(unit =>
        {
            CurrentUser = ctx.AuthorizedUser.Value;
            if (ctx.AuthorizedUser.IsNullOrUnassigned)
            {
                contextReadyCallback?.Invoke("Failed to login. Please try again later.");
            }
            else
            {
                ctx.Inventory.Refresh();
                ctx.Stats.Refresh();
                foreach (CurrencyRef currencyRef in Currencies)
                {
                    if (currencyRef.GetId() is { } id)
                    {
                        var playerCurrency = ctx.Inventory.GetCurrency(currencyRef);
                        _currencyAmount[id] = 0;
                        playerCurrency.OnAmountUpdated += amount =>
                        {
                            if (!_currencyAmount.TryGetValue(id, out var previousValue))
                            {
                                _currencyAmount[id] = previousValue = 0;
                            }

                            _currencyAmount[id] = amount;

                            (CurrencyRef currency, long PreviousValue, long NewValue) tuple = (currencyRef, previousValue,
                                amount);
                            CurrencyUpdateEvent?.Invoke(tuple);
                        };
                    }
                }
            
                async void GetStats()
                {
                    await FetchPublicClientStats();
                    await CurrentPlayerData.Validate();
                    contextReadyCallback?.Invoke(null);
                }

                GetStats();
            }
        }).Error(exception =>
        {
            contextReadyCallback?.Invoke("Failed to login. Reason: " + exception.Message);
        });
    }

    public List<TriggeRunItemData> GetDefaultItems()
    {
        return ItemDefinitions.Where(definition => definition.StarterItem).Select(definition => new TriggeRunItemData()
        {
            ItemId = definition.id,
            UnlockDate = DateTime.UtcNow,
        }).ToList();
    }

    public List<TriggeRunHeroData> GetDefaultHeroes()
    {
        return CharacterDefinitions.Where(definition => definition.StarterItem).Select(definition => new TriggeRunHeroData()
        {
            HeroId = definition.id,
            UnlockDate = DateTime.UtcNow,
        }).ToList();
    }

    public void LerpCurrencyText(TextMeshProUGUI coinCurrencyText, (CurrencyRef Currency, long PreviousValue, long NewValue) valueTuple)
    {
        Tween.Value(valueTuple.PreviousValue, valueTuple.NewValue, l =>
        {
            if (coinCurrencyText != null) coinCurrencyText.text = l.ToString("N0");
        }, (start, end, percent) => (long)Mathf.Lerp(start, end, percent), CurrencyTextLerpSettings).Play();
    }

    public void UpdateCurrency(CurrencyRef currencyRef, TextMeshProUGUI text)
    {
        text.text = (currencyRef.GetId() is {} id && _currencyAmount.TryGetValue(id, out var value) ? value : 0).ToString("N0");
    }

    public void UpdateCurrency(CurrencyLink currencyRef, TextMeshProUGUI text)
    {
        text.text = (currencyRef.GetId() is {} id && _currencyAmount.TryGetValue(id, out var value) ? value : 0).ToString("N0");
    }

    public bool TryAddCurrency(CurrencyRef currencyRef, int add)
    {
        if (!_currencyAmount.ContainsKey(currencyRef.GetId())) return false;
        
        _currencyAmount[currencyRef.GetId()] += add;
            
        currencyRef.Resolve().Then(content =>
        {
            // List the operations
            InventoryUpdateBuilder inventoryUpdateBuilder = new InventoryUpdateBuilder();
            inventoryUpdateBuilder.CurrencyChange(content.Id, add);
                
            // Execute the operations
            CurrentContext.Api.InventoryService.Update(inventoryUpdateBuilder).Then(obj =>
            {
                Debug.Log($"{add} AddCurrency() success.");
            });
        });

        return true;
    }
    
    public bool TryConsumeCurrency(CurrencyRef currencyRef, int consume)
    {
        if (!_currencyAmount.TryGetValue(currencyRef.GetId(), out var value)) return false;
        if (value < consume) return false;
                
        _currencyAmount[currencyRef.GetId()] -= consume;
        
        // remove it
        currencyRef.Resolve().Then(content =>
        {
            // List the operations
            InventoryUpdateBuilder inventoryUpdateBuilder = new InventoryUpdateBuilder();
            inventoryUpdateBuilder.CurrencyChange(content.Id, -consume);
                
            // Execute the operations
            CurrentContext.Api.InventoryService.Update(inventoryUpdateBuilder).Then(obj =>
            {
                Debug.Log($"{consume} RemoveCurrency() success.");
            });
        });
        return true;
    }

    public async Task<Dictionary<string, string>> GetPrivateGameStats() => await CurrentContext.Api.Stats.GetStats("game", "private", "player", CurrentContext.PlayerId);

    public async Task<Dictionary<string, string>> GetPublicGameStats() => await CurrentContext.Api.Stats.GetStats("game", "public", "player", CurrentContext.PlayerId);
    public async Task<Dictionary<string, string>> GetPublicClientStats()
    {
        return await CurrentContext.Api.Stats.GetStats("client", "public", "player", CurrentContext.PlayerId);
    }

    public async Task SetAndFetchPublicClientStats(Dictionary<string,string> data, bool doFetching, Action completeCallback = null)
    {
        await CurrentContext.Api.Stats.SetStats("public", data);
        
        
        if (doFetching) await FetchPublicClientStats();
        completeCallback?.Invoke();
    }
    
    public async void SetAndFetchPublicClientStatsVoid(Dictionary<string,string> data, bool doFetching, Action completeCallback = null)
    {
        await CurrentContext.Api.Stats.SetStats("public", data);
        if (doFetching) await FetchPublicClientStats();
        completeCallback?.Invoke();
    }

    public async Task FetchPublicClientStats(Action onFetchStats = null)
    {
        var fetch = await GetPublicClientStats();
        var stats = (fetch).ToDictionary(pair => pair.Key, pair => pair.Value.StartsWith("[") && pair.Value.EndsWith("]")
            ? JArray.Parse(pair.Value)
            : pair.Value.StartsWith("{") && pair.Value.EndsWith("}")
                ? JObject.Parse(pair.Value)
                : (object)pair.Value);
        CurrentPlayerData = stats.DeserializeJson<TriggeRunPlayerData>();
        onFetchStats?.Invoke();
    }

    public async void FetchPublicClientStatsVoid(Action onFetchStats = null)
    {
        await FetchPublicClientStats(onFetchStats);
    }
    

    public CharacterDefinition GetCurrentCharacter()
    {
        return CharacterDefinitions.Find(definition => definition.id == CurrentPlayerData.GetPreset().HeroId);
    }
    public CharacterDefinition GetCharacterById(int id)
    {
        return CharacterDefinitions.Find(definition => definition.id == id);
    }
    
    #if UNITY_EDITOR

    [Button]
    void Convert(TRCharacter character)
    {
        if (character == null) character = Character;
        CharacterDefinitions = CharacterContent.Characters.ToList();

        var allItemDefs = Resources.LoadAll<ItemDefinition>("Assets/_TriggeRun/_Dynamic Assets/Items/");

        if (character == null)
        {
            Debug.LogError("Character not set. Skipping adding all characters item!");
            return;
        }
        
        int incrId = allItemDefs.Length > 0 ? allItemDefs.Max(definition => definition.id + 1) : 0;
        
        UnityEditor.AssetDatabase.StartAssetEditing();

        character.GetComponentsInChildren<Item>(true).ToList().ForEach(item =>
        {
            var usableItem = item.GetComponent<UsableItem>();

            if (!(usableItem is ShootableWeapon weapon)) weapon = null;
            if (!(usableItem is ThrowableItem throwableItem)) throwableItem = null;
            if (!(usableItem is MagicItem magic)) magic = null;
            if (!(usableItem is MeleeWeapon melee)) melee = null;
            if (!(usableItem is Flashlight flashlight)) flashlight = null;

            if (usableItem == null)
            {
                // for now we ignore shield, since on a shield there is no usableweapon
                if (item.GetComponent<Shield>() == null)
                    Debug.LogError("Item doesn't have a shootable weapon: " + item);
                return;
            }

            var assetPath = $"Assets/_TriggeRun/_Dynamic Assets/Items/{item.ItemDefinition.name}.asset";
            ItemDefinition exists;

            
            // first find by our existing created assets
            exists = ItemDefinitions.Find(definition =>
                definition.MainItem != null && item.ItemDefinition != null &&
                definition.MainItem.CreateItemIdentifier().ID == item.ItemDefinition.CreateItemIdentifier().ID);
            
            // search by path
            if (exists == null)
            {
                exists = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemDefinition>(assetPath);
            }
            
            if (exists == null)
            {
                // if still not exists, we create one
                var obj = ScriptableObject.CreateInstance(nameof(ItemDefinition)) as ItemDefinition;
                if (obj != null)
                {
                    obj.id = ++incrId;
                    obj.Name = item.ItemDefinition.name;
                    obj.MainItem = item.ItemDefinition;
                    if (weapon != null)
                    {
                        obj.AmmoItem = weapon.ConsumableItemDefinition;
                        if (weapon.ConsumableItemDefinition is ItemType a) obj.AmmoAmount = a.Capacity;
                    }

                    UnityEditor.AssetDatabase.CreateAsset(obj, assetPath);
                }
            }
            else
            {
                exists.Name = item.ItemDefinition.name;
                if (weapon != null)
                {
                    exists.AmmoItem = weapon.ConsumableItemDefinition;
                    if (weapon.ConsumableItemDefinition is ItemType a) exists.AmmoAmount = a.Capacity;
                }
                
                
                var targetAssetPath = $"Assets/_TriggeRun/_Dynamic Assets/Items/{item.ItemDefinition.name}.asset";
                var currentAssetPath = UnityEditor.AssetDatabase.GetAssetPath(exists);
                if (currentAssetPath != targetAssetPath)
                {
                    Debug.Log($"Rename {currentAssetPath} to {assetPath}");
                    Debug.Log(UnityEditor.AssetDatabase.RenameAsset(currentAssetPath, Path.GetFileName(assetPath)));
                }



                UnityEditor.EditorUtility.SetDirty(exists);
            }
        });
        UnityEditor.AssetDatabase.StopAssetEditing();
        UnityEditor.AssetDatabase.SaveAssets();

        List<ItemDefinition> list = new List<ItemDefinition>();
        TryGetUnityObjectsOfTypeFromPath<ItemDefinition>("Assets/_TriggeRun/_Dynamic Assets/Items/", list);
        allItemDefs = list.ToArray();
        
        var itemsToChange = allItemDefs.GroupBy(x => x.id).Where(x => x.Count() > 1).SelectMany(x => x.Take(1)).ToList();
        if (itemsToChange.Count > 0)
        {
            foreach (ItemDefinition definition in itemsToChange)
            {
                definition.id = itemsToChange.Max(def => def.id + 1);
                UnityEditor.EditorUtility.SetDirty(definition);
                UnityEditor.AssetDatabase.SaveAssets();
            }
        }
        
        ItemDefinitions = allItemDefs.OrderBy(definition => definition.id).ToList();
        
        int TryGetUnityObjectsOfTypeFromPath<T>(string path, List<T> assetsFound) where T : UnityEngine.Object
        {
            string[] filePaths = System.IO.Directory.GetFiles(path);
 
            int countFound = 0;
 
            Debug.Log(filePaths.Length);
 
            if (filePaths != null && filePaths.Length > 0)
            {
                for (int i = 0; i < filePaths.Length; i++)
                {
                    UnityEngine.Object obj = UnityEditor.AssetDatabase.LoadAssetAtPath(filePaths[i], typeof(T));
                    if (obj is T asset)
                    {
                        countFound++;
                        if (!assetsFound.Contains(asset))
                        {
                            assetsFound.Add(asset);
                        }
                    }
                }
            }
 
            return countFound;
        }
    }
    #endif
}
