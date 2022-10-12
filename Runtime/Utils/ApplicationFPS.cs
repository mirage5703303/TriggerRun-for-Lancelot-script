using UnityEngine;

namespace IEdgeGames {

    public class ApplicationFPS : MonoBehaviour {

        [Tooltip("If true initialize on Awake, otherwise on Start.")]
        [SerializeField] private bool m_InitOnAwake = true;

        [Header("Target FPS")]
        [SerializeField, Range(30, 1000)] 
        private int m_StandaloneFPS = 1000;

        [SerializeField, Range(30, 120)]
        private int m_MobileFPS = 120;

        [Header("Mobile")]
        [SerializeField] private bool m_ScreenNeverSleep = true;

        void Awake() {
            if (m_InitOnAwake)
                Initialize();
        }

        void Start() {
            if (!m_InitOnAwake)
                Initialize();
        }

        void Initialize() {
            Application.targetFrameRate = Application.isMobilePlatform ? m_MobileFPS : m_StandaloneFPS;
            Screen.sleepTimeout = m_ScreenNeverSleep ? SleepTimeout.NeverSleep : SleepTimeout.SystemSetting;
        }

#if UNITY_EDITOR
        void OnValidate() => Initialize();
#endif
    }
}
