using UnityEngine;

namespace IEdgeGames {

    public class KillZone : MonoBehaviour {

        private int m_CheckLayer;

        void Awake() {
            m_CheckLayer = LayerMask.NameToLayer("Character");
        }

        void OnTriggerEnter(Collider other) {
            var colliderContainer = other.gameObject.transform.parent;
            CharacterHealthEx characterHealth;

            if (!colliderContainer
                || colliderContainer.gameObject.layer != m_CheckLayer
                || !(characterHealth = colliderContainer.GetComponentInParent<CharacterHealthEx>())
                || !characterHealth.IsAlive())
                return;

            characterHealth.Die();
        }
    }
}
