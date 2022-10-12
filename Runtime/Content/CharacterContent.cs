using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Sirenix.OdinInspector;

namespace IEdgeGames {

    [CreateAssetMenu(fileName = "_CharacterContent", menuName = "TriggeRun/Character Content")]
    public class CharacterContent : ScriptableObject {

        [SerializeField, InlineButton("ScanCharacters", "scan"), ValidateInput("ValidateCharacters")]
        private List<CharacterDefinition> m_Characters = new List<CharacterDefinition>();

        private List<CharacterDefinition> m_CharactersTmp;

        /// <summary>
        /// 
        /// </summary>
        [TableList]
        public static List<CharacterDefinition> Characters 
            => Instance && Instance.m_CharactersTmp != null && Instance.m_CharactersTmp.Count > 0
               ? Instance.m_CharactersTmp
               : Instance.m_CharactersTmp = Instance.m_Characters.Where(d => !d.disabled).ToList();

        /// <summary>
        /// 
        /// </summary>
        public static CharacterContent Instance {
            get {
                //return m_Instance ? m_Instance : m_Instance = Addressables.LoadAssetAsync<CharacterContent>("Characters/_CharacterContent.asset").WaitForCompletion();
                return m_Instance ? m_Instance : m_Instance = Resources.Load<CharacterContent>("_CharacterContent");
            }
        }

        private static CharacterContent m_Instance;

#if UNITY_EDITOR
        public List<Sprite> HeroesCards;
        [Button]
        private void ConvertAddressablesToDirectReferences()
        {
            int i = 0;
            m_Characters.ForEach(definition =>
            {
                definition.ConvertAddressablesToDirectReferences();
                definition.portrait = HeroesCards[i++ % HeroesCards.Count];
                UnityEditor.EditorUtility.SetDirty(definition);
            });
        }
        
        private bool ValidateCharacters()
        {
            // Ensure that the ids are unique.
            // Id starts at 1, since a new instance of a CharacterDefinition has a 0 id.
            if (m_Characters == null) return false;
            int newValidId = m_Characters.Max(definition => definition != null ? definition.id : 0) + 1;
            bool anyDirty = false;
            m_Characters.Where(definition => definition != null && definition.id == 0).ToList().ForEach(definition =>
            {
                definition.id = newValidId++;
                UnityEditor.EditorUtility.SetDirty(definition);
                anyDirty = true;
            });
            if (anyDirty) UnityEditor.AssetDatabase.SaveAssets();
            return true;
        }
        
        private void ScanCharacters() {
            /*var type = typeof(T);
            var flags = BindingFlags.NonPublic | BindingFlags.Static;
            var singletonName = type.GetProperty(nameof(SingletonName), flags)?.GetValue(null) as string ?? SingletonName;

            if (Application.isEditor) {
                var singletonFolder = type.GetProperty(nameof(SingletonFolder), flags)?.GetValue(null) as string ?? SingletonFolder;
                var directory = $"{Utils.ProjectPath}/{singletonFolder}";
                var assetPath = $"{directory}/{singletonName}.asset";

                if (!m_Instance)
                    m_Instance = AssetDatabase.LoadAssetAtPath<T>(assetPath);

                if (!m_Instance) {
                    m_Instance = CreateInstance<T>();
                    (m_Instance as DatabaseObject<T>).OnDirty();
                    Directory.CreateDirectory(directory);
                    AssetDatabase.CreateAsset(m_Instance, assetPath);
                    AssetDatabase.Refresh();
                }

                return m_Instance;
            }*/
        }
#endif
    }
}
