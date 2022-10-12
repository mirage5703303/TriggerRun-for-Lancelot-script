using UnityEngine;
using Opsive.UltimateCharacterController.Items;

namespace IEdgeGames {

    public class CharacterHierarchy : MonoBehaviour {

        public Transform hand_l;
        public Transform hand_r;

        [Header("Item Slots")]
        public ItemSlot slor_l;
        public ItemSlot slor_r;
    }
}
