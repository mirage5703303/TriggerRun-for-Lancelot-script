using System.Linq;
using UnityEngine;
using Sirenix.OdinInspector;
using Opsive.UltimateCharacterController.Objects.CharacterAssist;

namespace IEdgeGames {

    public class PickupGroup : MonoBehaviour {

        private static bool m_Initialized;

        void Awake() {
            if (m_Initialized)
                return;

            m_Initialized = true;
            Randomize();
        }

        void OnDisable() {
            m_Initialized = false;    
        }

        [Button]
        void Randomize() {
            var rnd = new System.Random();
            var groups = FindObjectsOfType<PickupGroup>().Select(g => g.transform);

            foreach (var group in groups) {
                var weapons = group.GetComponentsInChildren<ItemPickupBase>(true).ToList();

                group.transform.localEulerAngles = new Vector3(0f, Random.Range(-360f, 360f), 0f);

                foreach (var weapon in weapons)
                    weapon.gameObject.SetActive(false);

                for (var i = 0; i < 3; i++) {
                    var weapon = weapons[Random.Range(0, weapons.Count)];
                    weapon.gameObject.SetActive(true);
                    weapon.transform.localPosition = group.GetChild(i).localPosition;
                    weapons.Remove(weapon);
                }
            }
        }
    }
}
