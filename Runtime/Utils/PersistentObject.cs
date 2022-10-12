using UnityEngine;

namespace IEdgeGames {
	
	public class PersistentObject : MonoBehaviour {

		void Awake() => DontDestroyOnLoad(gameObject);
	}
}
