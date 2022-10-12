using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Sirenix.OdinInspector;

namespace IEdgeGames {
    [CreateAssetMenu(fileName = "CharacterDefinition", menuName = "TriggeRun/Character Definition")]
    public class CharacterDefinition : ScriptableObject 
    {
        /// <summary>
        /// Represent the unique id of the character
        /// </summary>
        [PropertyOrder(-2)]
        public int id;
        
        /// <summary>
        /// 
        /// </summary>
        [ShowInInspector, PropertyOrder(-1)]
        public new string name {
            get => base.name;
            set => base.name = value;
        }

        public string ListElementLabelName => $"[{id}] {name}";

        public AssetReferenceSprite icon;
        public AssetReferenceGameObject prefab;
        public AssetReferenceGameObject uiPrefab;
        public AssetReferenceGameObject brPrefab;
        
        public Sprite iconDirect;
        public GameObject prefabDirect;
        public GameObject uiPrefabDirect;
        public GameObject brPrefabDirect;

        public List<HeroSkinData> Skins = new List<HeroSkinData>();

        public Sprite portrait;

        public bool disabled;
        
        public bool StarterItem;

        public HeroClassType ClassType;
        
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
        
        
#if UNITY_EDITOR
        public void ConvertAddressablesToDirectReferences()
        {
            var tex2D = icon.editorAsset as Texture2D;
            if (tex2D != null)
                iconDirect = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(UnityEditor.AssetDatabase.GetAssetPath(tex2D));
            prefabDirect = prefab.editorAsset;
            uiPrefabDirect = uiPrefab.editorAsset;
            brPrefabDirect = brPrefab.editorAsset;
        }
#endif

        
        /// <summary>
        /// Check if the player has equipped the hero
        /// </summary>
        /// <returns></returns>
        public bool HasEquipped() => TriggeRunGameManager.Instance.CurrentPlayerData?.GetPreset()?.HeroId == id;
        
        /// <summary>
        /// Check if the player has owned the hero
        /// </summary>
        /// <returns></returns>
        public bool HasOwned() => TriggeRunGameManager.Instance.CurrentPlayerData?.heroes?.Any(data => data.HeroId == id) ?? false;
        
        public Dictionary<string, AttributeStatData> GetClassAttributes()
        {
            var max = TriggeRunGameManager.Instance.MaxClassStats;
            if (TriggeRunGameManager.Instance.ClassStats.TryGetValue(ClassType, out var current))
                return new Dictionary<string, AttributeStatData>()
                {
                    { "Class", new AttributeStatData(ClassType.ToString()) },
                    { "Strength", new AttributeStatData(current.Strength, max.Strength) },
                    { "Speed", new AttributeStatData(current.Speed, max.Speed) },
                    { "Agility", new AttributeStatData(current.Agility, max.Agility) },
                    { "Distance", new AttributeStatData(current.Distance, max.Distance) },
                    { "Skill", new AttributeStatData(current.Skill, max.Skill) },
                };

            return new Dictionary<string, AttributeStatData>();
        }

        public List<HeroSkinData> GetAllSkins()
        {
            return new List<HeroSkinData>()
            {
                new HeroSkinData()
                {
                    SkinSprite = iconDirect,
                    Starter = true,
                }
            }.Concat(Skins ?? new List<HeroSkinData>()).ToList();
        }

        public Sprite GetSkinByIndex(int skinIndex)
        {
            return GetAllSkins()[skinIndex].SkinSprite;
        }
    }

    [Serializable]
    public class AttributeStatData
    {
        public string ValueText;
        public float CurrentValue;
        public float MaxValue;

        public string Affix = "";
        
        public AttributeStatData(string toString)
        {
            ValueText = toString;
        }

        public AttributeStatData()
        {
        }

        public AttributeStatData(float currentValue, float maxValue)
        {
            CurrentValue = currentValue;
            MaxValue = maxValue;
        }


        public string GetString()
        {
            return !string.IsNullOrEmpty(ValueText) ? ValueText : CurrentValue.ToString("F1");
        }
        public float GetAlpha()
        {
            if (MaxValue <= 0) return 0.0f;
            return 1.0f / MaxValue * CurrentValue;
        }
    }

    [Serializable]
    public class HeroSkinData
    {
        public Sprite SkinSprite;
        public int CoinCosts = -1;
        public int GemCosts = 1000;
        public bool Starter { get; set; }

        public bool HasSkin(CharacterDefinition characterDefinition, int skinIndex)
        {
            if (Starter) return true;
            if (characterDefinition == null) return false;
            var hero = TriggeRunGameManager.Instance.CurrentPlayerData.heroes.Find(data =>
                data.HeroId == characterDefinition.id);
            if (hero == null) return false;
            return (hero.OwnedSkins == SkinOwnedEnum.Default && skinIndex == 0) || hero.OwnedSkins.HasFlag((SkinOwnedEnum)(1 << skinIndex));
        }
    }

    [Serializable]
    public class HeroClassStatsData
    {
        public float Strength;
        public float Speed;
        public float Agility;
        public float Distance;
        public float Skill;
    }
    
    public enum HeroClassType
    {
        Damager,
        Healer,
        Tank,
        Smart,
    }
}
