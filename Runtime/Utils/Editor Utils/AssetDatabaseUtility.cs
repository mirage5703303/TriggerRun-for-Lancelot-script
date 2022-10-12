#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace IEdgeGamesEditor {

    public static class AssetDatabaseUtility {

        /// <summary>
        /// Returns all assets at given path.
        /// </summary>
        /// <param name="path"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T[] LoadAllAssetsInFolder<T>(string path) where T : Object 
            => AssetDatabase.FindAssets("t:" + typeof(T).Name)
                            .Select(a => AssetDatabase.GUIDToAssetPath(a))
                            .Where(a => a.StartsWith(path))
                            .Select(a => AssetDatabase.LoadAssetAtPath<T>(a))
                            .ToArray();
    }
}
#endif
