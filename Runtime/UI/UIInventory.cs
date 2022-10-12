using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;

namespace IEdgeGames {

    public class UIInventory : MonoBehaviour {

        [SerializeField] private Transform m_AvatarContainer;
        [SerializeField] private GameObject m_ButtonPrefab;
        [SerializeField] private Transform m_ButtonContainer;
        [SerializeField] private Button m_InventoryButton;
        [SerializeField] private Button m_BackButton;

        public bool buttonscharacters = false;
        private GameObject m_LastCharacter;

        // ============================================================================================================================

        void Awake() {
            if (m_ButtonContainer)
                m_ButtonContainer.gameObject.GetChilds().Destroy();

            m_AvatarContainer.gameObject.GetChilds().Destroy();

            if (buttonscharacters) {
                foreach (var characterDefinition in CharacterContent.Characters) {
                    var button = Instantiate(m_ButtonPrefab);
                    var cd = characterDefinition;

                    if (m_ButtonContainer)
                        button.transform.SetParent(m_ButtonContainer);

                    var graphicContent = button.transform.GetChild(0).GetChild(0);

                    graphicContent.GetComponent<Image>().sprite = cd.iconDirect;
                    // graphicContent.GetComponent<Image>().sprite = cd.icon.Asset ? (Sprite)cd.icon.Asset : cd.icon.LoadAssetAsync().WaitForCompletion();

                    graphicContent.GetComponent<Button>().onClick.AddListener(() => {
                        if (m_LastCharacter)
                        {
                            //Addressables.ReleaseInstance(m_LastCharacter);
                            Destroy(m_LastCharacter);
                        }

                        // (m_LastCharacter = cd.uiPrefab.InstantiateAsync(m_AvatarContainer).WaitForCompletion())
                        (m_LastCharacter = Instantiate(cd.uiPrefabDirect))
                            .SetLayerRecursively("UI");
                        //TRPlayer.CharacterId = cd.id;
                    });
                }
            }


            m_InventoryButton.onClick.AddListener(() => {
                if (!m_LastCharacter)
                    return;

                var animator = m_LastCharacter.GetComponent<Animator>();
                animator.Play("Base Layer.Idle");
            });

            m_BackButton.onClick.AddListener(() => {
                if (!m_LastCharacter)
                    return;

                var animator = m_LastCharacter.GetComponent<Animator>();
                animator.Play("Base Layer.Walk");
            });

            var currentCharacter = TRPlayer.Character;

            if (!currentCharacter)
                return;

            // (m_LastCharacter = currentCharacter.uiPrefab.InstantiateAsync(m_AvatarContainer).WaitForCompletion())
            (m_LastCharacter = Instantiate(currentCharacter.uiPrefabDirect, m_AvatarContainer))
                .SetLayerRecursively("UI");

            if (m_LastCharacter) {
                var animator = m_LastCharacter.GetComponent<Animator>();
                animator.Play("Base Layer.Walk");
            }
        }
    }
}
