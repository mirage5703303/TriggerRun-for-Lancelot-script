using UnityEngine;
using UnityEngine.UI;

namespace IEdgeGames {

    public class UITermAndConditions : UIModule<UITermAndConditions> {

        [Header("Buttons")]
        [SerializeField] private Button m_AcceptButton;
        [SerializeField] private Button m_CancelButton;

        void Awake() {
            m_AcceptButton.onClick.AddListener(() => {
                _deprecated_UIAutentication.Active = true;
            });

            if (m_CancelButton)
                m_CancelButton.onClick.AddListener(() => {
                    // TODO
                });
        }
    }
}
