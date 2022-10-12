using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace IEdgeGames {

    public class UIConnectionStatusPUN : UIModule<UIConnectionStatusPUN> {

        [Header("UI Components")]
        [SerializeField] private TextMeshProUGUI m_Label;
        [SerializeField] private Button m_Connect;

        /// <summary>
        /// 
        /// </summary>
        public static string Text {
            get => Instance && Instance.m_Label ? Instance.m_Label.text : "";
            set {
                if (!Instance || !Instance.m_Label)
                    return;

                Instance.m_Label.text = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public static Color LabelColor {
            get => Instance && Instance.m_Label ? Instance.m_Label.color : default;
            set {
                if (!Instance || !Instance.m_Label)
                    return;

                Instance.m_Label.color = value;
            }
        }

        void Awake() {
            m_Connect.gameObject.SetActive(Application.isEditor);

            m_Connect.onClick.AddListener(() => {
                m_Connect.interactable = false;
                Text = "Connecting...";
                Matchmaking.Connect(false);
            });
        }
    }
}
