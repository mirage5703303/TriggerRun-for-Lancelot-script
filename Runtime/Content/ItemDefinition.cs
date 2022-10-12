using System;
using System.Collections.Generic;
using System.Linq;
using Opsive.Shared.Inventory;
using Sirenix.OdinInspector;
using UnityEngine;

namespace IEdgeGames
{
    [CreateAssetMenu(fileName = "ItemDefinition", menuName = "TriggeRun/Item Definition")]
    public class ItemDefinition : ScriptableObject
    {
        /// <summary>
        /// Represent the unique id of the character
        /// </summary>
        [PropertyOrder(-2)] public int id;
        
        public string Name;

        public Sprite ItemSprite;
        public List<ItemSkinData> Skins = new List<ItemSkinData>();

        public ItemStatsData StatsData;
        
        public ItemDefinitionBase MainItem;
        
        // the ammo
        public ItemDefinitionBase AmmoItem;
        public int AmmoAmount;
        
        public bool StarterItem;

        public ItemSlotCategory SlotCategory;
        public ItemLevelType ItemLevelType;

        /// <summary>
        /// The p2e currency.
        /// Use -1 if it's not purchasable with that currency
        /// </summary>
        public int CoinCosts;
        
        /// <summary>
        /// The Premium currency
        /// Use -1 if it's not purchasable with that currency
        /// </summary>
        public int GemCosts;
        
        public DateTime ReleaseDate;
        
        public bool Disabled;

        public string ListElementLabelName => $"[{id} - {SlotCategory}] {Name}";
        
        /// <summary>
        /// Check if we have the item equipped
        /// </summary>
        /// <returns></returns>
        public bool HasEquipped() => TriggeRunGameManager.Instance.CurrentPlayerData?.GetPreset()?.ItemIds?.Contains(id) ?? false;
        
        /// <summary>
        /// Check if we own the item
        /// </summary>
        /// <returns></returns>
        public bool HasOwned() => TriggeRunGameManager.Instance.CurrentPlayerData?.items?.Any(data => data.ItemId == id) ?? false;
        
        
        public Dictionary<string, AttributeStatData> GetStatsAttributes()
        {
            var max = TriggeRunGameManager.Instance.MaxItemStats;
            return new Dictionary<string, AttributeStatData>()
            {
                { "Sight", new AttributeStatData(StatsData.Sight, max.Sight) },
                { "Load", new AttributeStatData(StatsData.Load, max.Load)},
                { "Grip", new AttributeStatData(StatsData.Grip, max.Grip)},
                { "Recoil", new AttributeStatData(StatsData.Recoil, max.Recoil) },
                { "Fire Rate", new AttributeStatData(StatsData.FireRate, max.FireRate)
                {
                    Affix = "/s"
                } },
            };
        }
        

        public List<ItemSkinData> GetAllSkins()
        {
            return new List<ItemSkinData>()
            {
                new ItemSkinData()
                {
                    SkinSprite = ItemSprite,
                    Starter = true,
                }
            }.Concat(Skins ?? new List<ItemSkinData>()).ToList();
        }

        public Sprite GetSkinByIndex(int skinIndex)
        {
            return GetAllSkins()[skinIndex].SkinSprite;
        }
    }
    
    [Serializable]
    public class ItemStatsData
    {
        public float Sight;
        public float Load;
        public float Grip;
        public float Recoil;
        public float FireRate;
    }
    
    [Serializable]
    public class ItemSkinData
    {
        public Sprite SkinSprite;
        public int CoinCosts = -1;
        public int GemCosts = 1000;
        public bool Starter { get; set; }

        public bool HasSkin(ItemDefinition itemDefinition, int skinIndex)
        {
            if (Starter) return true;
            if (itemDefinition == null) return false;
            var item = TriggeRunGameManager.Instance.CurrentPlayerData.items.Find(data =>
                data.ItemId == itemDefinition.id);
            if (item == null) return false;
            return (item.OwnedSkins == SkinOwnedEnum.Default && skinIndex == 0) || item.OwnedSkins.HasFlag((SkinOwnedEnum)(1 << skinIndex));
        }
    }

    /// <summary>
    /// Indicates the flair color of the item (most likely on the top-left)
    /// </summary>
    public enum ItemLevelType
    {
        Free,
        Rookie,
        Amateur,
        Semi,
        Pro,
    }
    
    [Flags]
    public enum ItemSlotCategory
    {
        None,
        
        Rifle = 1 << 0,
        Rocket = 1 << 1,
        Shotgun = 1 << 2,
        Sniper = 1 << 3,
        Bow = 1 << 4,
        Reserved2 = 1 << 5,
        Reserved3 = 1 << 6,
        Reserved4 = 1 << 7,
        
        Pistol = 1 << 8,
        Melee = 1 << 9,
        Throwable = 1 << 10,
        Skill = 1 << 11,
    }
}