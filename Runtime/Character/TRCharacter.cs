using System.Linq;
using System.Reflection;
using Opsive.Shared.Events;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
using Opsive.UltimateCharacterController.Camera;
using Opsive.UltimateCharacterController.ThirdPersonController.Items;
using Opsive.UltimateCharacterController.FirstPersonController.Items;
using Opsive.UltimateCharacterController.Traits.Damage;
using Photon.Pun;
using Sirenix.OdinInspector;

namespace IEdgeGames {

    public class TRCharacter : MonoBehaviourPun, IDamageOriginator {

        // ============================================================================================================================

        [SerializeField] private string m_CharacterName;

        [Header("Properties")]
        [SerializeField] private Vector3 m_CameraOffset = new Vector3(0f, 1.8f, 0f);

        private CameraController m_CameraController;

        // ============================================================================================================================

        /// <summary>
        /// 
        /// </summary>
        public string CharacterName => m_CharacterName;


        [ShowIf("@UnityEngine.Application.isPlaying"), ShowInInspector] public TriggeRunPresetData CurrentPreset { get; set; }
        [ShowIf("@UnityEngine.Application.isPlaying"), ShowInInspector] public bool IsMine { get; set; }
        [ShowIf("@UnityEngine.Application.isPlaying"), ShowInInspector] public bool IsAI { get; set; }
        [ShowIf("@UnityEngine.Application.isPlaying"), ShowInInspector] public int PlayerIndex { get; set; }
        [ShowIf("@UnityEngine.Application.isPlaying"), ShowInInspector] public PlayerTeam Team { get; set; }

        public GameObject Owner => gameObject;
        public GameObject OriginatingGameObject => gameObject;
        
        // ============================================================================================================================

        void Awake() {
            m_CameraController = FindObjectOfType<CameraController>();
            m_CameraController.AnchorOffset = m_CameraOffset;
#if UNITY_EDITOR
            LockWeapons(true);
#endif

            var respawner = GetComponent<CharacterRespawnerEx>();
            if (respawner != null) respawner.OnRespawnEvent.AddListener(OnRespawn);
        }

        private void OnRespawn()
        {
            if (IsMine && GameSession.Instance != null)
            {
                Utils.LockCursor(GameSession.Instance.GameEnded);
                
                var localPlayer = Utils.GetLocalPlayer();
                if (localPlayer)
                    Opsive.Shared.Events.EventHandler.ExecuteEvent(localPlayer.gameObject, "OnEnableGameplayInput", !GameSession.Instance.GameEnded);
            }
        }

        void OnDestroy() {
#if UNITY_EDITOR
            LockWeapons(false);
#endif
            Addressables.ReleaseInstance(gameObject);
        }

        void Update() {
            // Temp code
            if (Input.GetKeyDown(KeyCode.Tab)) {
                Utils.LockCursor(false);
                UIManager.Instance.ShowOnly<LoadingMenu>().WaitForSceneLoad(null, 0, () =>
                {
                    UIManager.Instance.ShowOnly<MainMenu>().FadeIn();
                });
                SceneManager.LoadScene(0);
            }
        }

        // ============================================================================================================================

        
        // ============================================================================================================================

#if UNITY_EDITOR

        void OnValidate() {
            if (Application.isPlaying || UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            if (!gameObject.scene.IsValid() 
                && (string.IsNullOrEmpty(m_CharacterName) 
                || (m_CharacterName == "_CharacterBase" && name != "_CharacterBase") 
                || m_CharacterName.ToLower().EndsWith(" variant")))
                m_CharacterName = name;
        }

        void LockWeapons(bool flag) {
            if (!ProjectParameters.LockWeapons)
                return;

            var itemContainer = transform.Find("Items");

            if (!itemContainer)
                return;

            foreach (Transform t in itemContainer) {
                var tps_weapon = t.GetComponent<ThirdPersonShootableWeaponProperties>();
                var fps_weapon = t.GetComponent<FirstPersonShootableWeaponProperties>();

                if (!tps_weapon || !fps_weapon)
                    continue;

                var lockValue = flag ? 1 : -1;
                tps_weapon.LookSensitivity = lockValue;

                var fpsLS = typeof(FirstPersonShootableWeaponProperties).GetField("m_LookSensitivity", BindingFlags.NonPublic | BindingFlags.Instance);
                fpsLS.SetValue(fps_weapon, lockValue);
            }
        }
#endif
        public void SetPreset(TriggeRunPresetData preset)
        {
            CurrentPreset = preset;
        }
    }
}
