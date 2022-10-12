using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace IEdgeGames {

    public class RuntimeAssets : SingletonBehaviour<MonoBehaviour> {

        [SerializeField] private bool m_PreloadAssetsOnAwake;

        //private static readonly Dictionary<> m_Cache = 

        protected override void SingletonAwake() {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public static AsyncOperationHandle<T> LoadAsync<T>(string key, Action<T> result = null) where T : UnityEngine.Object {
            var handle = Addressables.LoadAssetAsync<T>(key);
            handle.Completed += (op) => result?.Invoke(op.Result);
            return handle;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="reference"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public static AsyncOperationHandle<T> LoadAsync<T>(AssetReference reference, Action<T> result = null) where T : UnityEngine.Object {
            var handle = reference.LoadAssetAsync<T>();
            handle.Completed += (op) => result?.Invoke(op.Result);
            return handle;
        }
    }
}
