#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace IEdgeGamesEditor {
    
    public static class EditorResources {

        /// <summary>
        /// Loads an asset at path in a Editor Resources folder.
        /// </summary>
        /// <param name="resourcePath"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T Load<T>(string resourcePath) where T : Object {
            var assetPath = AssetDatabase.GetAllAssetPaths().ToList()
                                         .Find(p => p.EndsWith("/Editor Resources/" + resourcePath));

            return string.IsNullOrEmpty(assetPath) ? default : AssetDatabase.LoadAssetAtPath<T>(assetPath);
        }
    }
}
#endif
