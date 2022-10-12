using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using System.IO;
using UnityEditor;
#endif

namespace IEdgeGames {

    public abstract class SingletonScriptableObject<T> : ScriptableObject where T : ScriptableObject {

        private static T m_Instance;

        /// <summary>
	    /// Singleton instance.
	    /// </summary>
        public static T Instance => m_Instance ? m_Instance : (m_Instance = Resources.LoadAll<T>("").FirstOrDefault());

        /// <summary>
        /// Preloads singleton instance.
        /// </summary>
        public static void Preload() {
            if (!m_Instance)
                m_Instance = Resources.LoadAll<T>("").FirstOrDefault();
        }

#if UNITY_EDITOR
        /// <summary>
        /// 
        /// </summary>
        /// <param name="resourcesPath"></param>
        /// <returns></returns>
        public static T GetOrCreateInstance(string resourcesPath) {
            var instance = Instance;

            if (!instance) {
                instance = CreateInstance<T>();
                AssetDatabase.CreateAsset(instance, Path.Combine(resourcesPath, typeof(T).Name + ".asset"));
            }

            return instance;
        }
#endif
    }
}
