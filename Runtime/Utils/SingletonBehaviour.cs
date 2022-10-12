using UnityEngine;

namespace IEdgeGames {
    
    public abstract class SingletonBehaviour<T> : MonoBehaviour where T : MonoBehaviour {

	    protected static T m_Instance;
	 
	    /// <summary>
	    /// Singleton instance.
	    /// </summary>
	    public static T Instance => m_Instance ? m_Instance : (m_Instance = FindObjectOfType<T>());

        private bool Initialize() {
			if (m_Instance == this)
				return true;

			if (m_Instance && m_Instance != this) {
				Debug.LogWarning($"Multiple {typeof(T).Name} detected in the scene. Destroying duplicates.");
				Destroy(gameObject);
				return false;
			}

			m_Instance = FindObjectOfType<T>();
			return true;
		}

        protected virtual void Start() {
			if (Initialize())
				SingletonStart();
		}

		protected virtual void Awake() {
			if (Initialize())
				SingletonAwake();
		}

		protected virtual void OnDestroy() {
			if (m_Instance == this) {
				SingletonDestroy();
				m_Instance = null;
			}
		}

		/// <summary>
		/// Called on initialize singleton isntance.
		/// </summary>
		protected virtual void SingletonStart() { }

		/// <summary>
		/// Called on initialize singleton isntance.
		/// </summary>
		protected virtual void SingletonAwake() { }

		/// <summary>
		/// Called when singleton instance is destroyed.
		/// </summary>
		protected virtual void SingletonDestroy() { }
	}
}
