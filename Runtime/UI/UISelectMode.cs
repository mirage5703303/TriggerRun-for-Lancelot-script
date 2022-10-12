using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using TMPro;
using Photon.Realtime;

namespace IEdgeGames {

    public class UISelectMode : MonoBehaviour {

        [Serializable]
        private class ButtonInfo {
            public GameMode mode;
            public Button button;
        }

        // ============================================================================================================================

        [SerializeField] private int m_AcceptMatchCountdown = 30;
        [SerializeField] private bool m_AutoAcceptMatch;

        [Header("Panels")]
        //[SerializeField] private GameObject m_Panel;
        [SerializeField] private GameObject m_FindMatchPanel;
        [SerializeField] private GameObject m_AcceptMatchPanel;
        [SerializeField] private UIPanelLobby m_LobbyPanel;

        [Header("Labels")]
        //[SerializeField] private TextMeshProUGUI m_ConnectionStatus;
        [SerializeField] private TextMeshProUGUI m_MatchingTime;
        [SerializeField] private TextMeshProUGUI m_AcceptCountLabel;
        [SerializeField] private TextMeshProUGUI m_AcceptCountdown;

        [Header("Buttons")]
        //SerializeField] private Button m_Connect;
        [SerializeField] private Button m_TDM;
        [SerializeField] private Button m_BattleRoyale;
        [SerializeField] private Button m_AcceptMatch;
        [SerializeField] private Button m_CancelMatch;
        [SerializeField] private Button m_PlaySolo;
        [SerializeField] private ButtonInfo[] m_ButtonInfo;

        private DateTime m_StartTime;
        private bool m_InitializeOfflineMode;
        private bool m_FindingMatch;

        // ============================================================================================================================

        /// <summary>
        /// 
        /// </summary>
        public static GameMode SelectedMode { get; set; }

        /// <summary>
        /// Is the current gamemode TDM?
        /// </summary>
        public static bool IsTeamDeathMatch => SelectedMode >= GameMode.VS1 && SelectedMode <= GameMode.VS6;

        // ============================================================================================================================

        void Awake() {
            if (PhotonNetwork.IsConnected)
                OnConnectToMaster();
            else
                OnDisconnect(DisconnectCause.DisconnectByClientLogic);

            foreach (var info in m_ButtonInfo) {
                var mode = info.mode;
                info.button.onClick.AddListener(() => FindMatch(mode));
            }

            if (m_AcceptMatch)
                m_AcceptMatch.onClick.AddListener(AcceptMatch);
            if (m_CancelMatch)
                m_CancelMatch.onClick.AddListener(Matchmaking.CancelMatch);

            /*m_TDM.onClick.AddListener(() =>
            {
                SelectedMode = GameMode.VS4;
                FindMatch(SelectedMode);
            });*/
            
            m_PlaySolo.onClick.AddListener(() => {
                SelectedMode = GameMode.PlaySolo;
                m_InitializeOfflineMode = true;
                Matchmaking.Connect(true, () => Matchmaking.PlaySolo());
            });

            if (m_BattleRoyale)
                m_BattleRoyale.onClick.AddListener(() => Matchmaking.FindMatch(GameMode.BattleRoyale));
        }

        void OnEnable() {
            Matchmaking.OnConnectToMaster += OnConnectToMaster;
            Matchmaking.OnDisconnect += OnDisconnect;
            Matchmaking.OnCancelMatch += OnCancelMatch;
            Matchmaking.OnFillRoom += OnFillRoom;
            Matchmaking.OnPlayerReady += OnPlayerReady;
            Matchmaking.OnBeginLoadLevel += OnBeginLoadLevel;
            Matchmaking.OnPlayerJoined += OnPlayerJoined;
            Matchmaking.OnPlayerEnteredRoomEvent += OnPlayerEnteredRoomEvent;
        }

        void OnDisable() {
            Matchmaking.OnConnectToMaster -= OnConnectToMaster;
            Matchmaking.OnDisconnect -= OnDisconnect;
            Matchmaking.OnCancelMatch -= OnCancelMatch;
            Matchmaking.OnFillRoom -= OnFillRoom;
            Matchmaking.OnPlayerReady -= OnPlayerReady;
            Matchmaking.OnBeginLoadLevel -= OnBeginLoadLevel;
            Matchmaking.OnPlayerJoined -= OnPlayerJoined;
            Matchmaking.OnPlayerEnteredRoomEvent -= OnPlayerEnteredRoomEvent;
        }

        // ============================================================================================================================

        void FindMatch(GameMode mode) {
            m_FindingMatch = true;
            Matchmaking.FindMatch(SelectedMode = mode);
            StartCoroutine(ShowFindMatchPanel());
        }

        void AcceptMatch() {
            m_FindingMatch = false;
            m_AcceptMatch.interactable = false;
            Matchmaking.SetPlayerReady();
        }

        IEnumerator ShowFindMatchPanel() {
            m_StartTime = DateTime.Now;
            m_MatchingTime.text = "00:00";
            m_FindMatchPanel.SetActive(true);
            m_AcceptMatchPanel.SetActive(false);

            if (IsTeamDeathMatch)
            {
                m_LobbyPanel.gameObject.SetActive(true);
                m_LobbyPanel.UpdateLobby(new Dictionary<int, Player>(), false);
            }
            else
            {
                m_LobbyPanel.gameObject.SetActive(false);
            }

            yield return new WaitForSeconds(.1f);

            while (true) {
                m_MatchingTime.text = (DateTime.Now - m_StartTime).ToString(@"mm\:ss");
                yield return new WaitForSeconds(1f);
            }
        }

        IEnumerator AcceptCountdown() {
            var countdown = m_AcceptMatchCountdown;

            m_AcceptMatch.interactable = true;

            while (countdown > 0) {
                m_AcceptCountdown.text = $"{countdown}";
                countdown--;
                yield return new WaitForSeconds(1f);
            }

            if (m_TDM)
                m_TDM.interactable = true;
            Matchmaking.CancelMatch();
        }

        // ============================================================================================================================

        void OnConnectToMaster() {
            if (m_FindingMatch)
                return;

            foreach (var info in m_ButtonInfo)
                info.button.interactable = true;

            m_FindMatchPanel.SetActive(false);
            m_AcceptMatchPanel.SetActive(false);
            m_LobbyPanel.gameObject.SetActive(false);
            if (m_InitializeOfflineMode)
                m_InitializeOfflineMode = false;
        }

        void OnDisconnect(DisconnectCause cause) {
            if (cause == DisconnectCause.DisconnectByClientLogic)
                return;

            foreach (var info in m_ButtonInfo)
                info.button.interactable = false;

            StopAllCoroutines();

            m_FindingMatch = false;
            m_FindMatchPanel.SetActive(false);
            m_AcceptMatchPanel.SetActive(false);
            m_LobbyPanel.gameObject.SetActive(false);
        }

        void OnCancelMatch() {
            StopAllCoroutines();
            m_FindMatchPanel.SetActive(false);
            m_AcceptMatchPanel.SetActive(false);
            m_LobbyPanel.gameObject.SetActive(false);
        }

        void OnFillRoom() {
            StopAllCoroutines();

            m_FindMatchPanel.SetActive(false);
            m_AcceptMatchPanel.SetActive(true);

            m_AcceptCountLabel.text = $"{Matchmaking.CurrentRoomPlayerCount} / {Matchmaking.CurrentRoomMaxPlayers}";
            
            Debug.Log($"[OnFillRoom] Bots to create: {(Matchmaking.CurrentRoomMaxPlayers - Matchmaking.CurrentRoomPlayerCount)}");

            if (m_AutoAcceptMatch)
            {
                m_AcceptMatch.gameObject.SetActive(false);
                AcceptMatch();
            }
            else
            {
                m_AcceptMatch.gameObject.SetActive(true);
                StartCoroutine(AcceptCountdown());
            }
            
            
            m_LobbyPanel.UpdateLobby(PhotonNetwork.CurrentRoom.Players, true);
        }

        void OnPlayerReady(Player player, int readyPlayersCount, int maxPlayers) {
            m_AcceptCountLabel.text = $"{Matchmaking.CurrentRoomPlayerCount} / {Matchmaking.CurrentRoomMaxPlayers}";
            
            m_LobbyPanel.UpdateLobby(PhotonNetwork.CurrentRoom.Players, true);
        }

        void OnBeginLoadLevel(int i) {
            if (!this || !enabled || !gameObject.activeInHierarchy || !gameObject.activeSelf)
                return;

            StopAllCoroutines();
        }
        
        private void OnPlayerJoined(Dictionary<int, Player> obj)
        {
            m_LobbyPanel.UpdateLobby(obj, false);
        }
        private void OnPlayerEnteredRoomEvent(Player obj)
        {
            m_LobbyPanel.UpdateLobby(PhotonNetwork.CurrentRoom.Players, false);
        }
    }
}
