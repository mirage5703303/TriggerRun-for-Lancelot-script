using System.Collections.Generic;
using UnityEngine;

namespace IEdgeGames {

    public class SerializedDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver {
	
        [SerializeField, HideInInspector]
        private List<TKey> m_keyData = new List<TKey>();
        
        [SerializeField, HideInInspector]
        private List<TValue> m_valueData = new List<TValue>();

        public SerializedDictionary() { }

        public SerializedDictionary(IDictionary<TKey, TValue> dictionary) {
            if (null != dictionary)
                foreach (var pair in dictionary)
                    this[pair.Key] = pair.Value;
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize() {
            Clear();
            
            for (var i = 0; i < m_keyData.Count && i < m_valueData.Count; i++)
                this[m_keyData[i]] = m_valueData[i];
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize() {
            m_keyData.Clear();
            m_valueData.Clear();

            foreach (var item in this) {
                m_keyData.Add(item.Key);
                m_valueData.Add(item.Value);
            }
        }
    }
}
