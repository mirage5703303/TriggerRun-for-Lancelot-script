using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using TrickCore;
using PhotonHashtable = ExitGames.Client.Photon.Hashtable;

namespace IEdgeGames {

    public class Matchmaking : MonoBehaviourPunCallbacks {

        // ============================================================================================================================

        [Header("Timeouts")]
        [SerializeField, Range(0, 60)]
        private int m_AutoFillMatchTimeout = 20;

        [SerializeField, Range(1, 10)]
        private int m_AttemptConnectionTimeout = 2;

        // ============================================================================================================================

        /// <summary>
        /// Singleton instance.
        /// </summary>
        public static Matchmaking Instance => m_Instance ? m_Instance : (m_Instance = FindObjectOfType<Matchmaking>());

        /// <summary>
        /// 
        /// </summary>
        public static SceneReference SelectedMap { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public static int CurrentRoomPlayerCount {
            get {
                if (!PhotonNetwork.InRoom)
                    return 0;

                var ai_count = PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(RoomProps.AI_COUNT, out object _ai_count) ? (int)_ai_count : 0;
                return PhotonNetwork.CurrentRoom.PlayerCount + ai_count;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public static int CurrentRoomMaxPlayers => PhotonNetwork.InRoom ? PhotonNetwork.CurrentRoom.MaxPlayers : 0;

        /// <summary>
        /// 
        /// </summary>
        public static event Action OnConnectToMaster = delegate { };

        /// <summary>
        /// 
        /// </summary>
        public static event Action<DisconnectCause> OnDisconnect = delegate { };

        /// <summary>
        /// 
        /// </summary>
        public static event Action OnCancelMatch = delegate { };

        /// <summary>
        /// 
        /// </summary>
        public static event Action OnFillRoom = delegate { };

        /// <summary>
        /// 
        /// </summary>
        public static event Action OnRoomReady = delegate { };

        /// <summary>
        /// 
        /// </summary>
        public static event Action<Player, int, int> OnPlayerReady = delegate { };

        /// <summary>
        /// 
        /// </summary>
        public static event Action<int> OnBeginLoadLevel = delegate { };
        
        public static event Action<Dictionary<int, Player>> OnPlayerJoined = delegate { };
        public static event Action<Dictionary<int, Player>> OnPlayerLeft = delegate { };
        public static event Action<Player> OnPlayerEnteredRoomEvent = delegate { };

        // ============================================================================================================================

        private Action m_OnConnectToMasterInternal;

        private GameMode m_RequestedMode = (GameMode)(-1);
        protected int m_RoomStartTime;
        protected int m_RoomCurrentTime;
        protected bool m_MatchRequested;
        protected bool m_AutoSetPlayerReady;

        private static Matchmaking m_Instance;
        private static readonly RaiseEventOptions m_RaiseEventAll = new RaiseEventOptions { Receivers = ReceiverGroup.All };

        // ============================================================================================================================

        protected virtual void Start() {
            if (!m_MatchRequested)
                Connect(Application.isEditor);

            PhotonNetwork.SendRate = 64;
            PhotonNetwork.SerializationRate = 32;
        }

        public override void OnEnable() {
            base.OnEnable();
            PhotonNetwork.NetworkingClient.EventReceived += OnEvent;
        }

        public override void OnDisable() {
            base.OnDisable();
            PhotonNetwork.NetworkingClient.EventReceived -= OnEvent;
        }

        // ============================================================================================================================

        public override void OnConnectedToMaster() {
            Debug.Log($"PUN: <Color=Green><b>OnConnectedToMaster</b></color>");

            // For now if we are inside the editor, make the timeout longer, so we can actually debug without disconnecting
            if (Application.isEditor)
                PhotonNetwork.NetworkingClient.LoadBalancingPeer.DisconnectTimeout = 65535;
                
            m_OnConnectToMasterInternal?.Invoke();
            OnConnectToMaster?.Invoke();
            m_OnConnectToMasterInternal = null;
        }

        public override void OnDisconnected(DisconnectCause cause) {
            if (PhotonNetwork.OfflineMode || cause.ToString() == "DisconnectByClientLogic")
                return;

            Debug.Log($"PUN: <Color=Red><b>OnDisconnected: {cause}</b></color>");

            StopAllCoroutines();
            OnDisconnect?.Invoke(cause);

            // Attempt connect again
            this.Sleep(m_AttemptConnectionTimeout, () => {
                Debug.Log($"PUN: Try connecting...");
                Connect(false);
            });
        }

        // ============================================================================================================================

        void OnEvent(EventData photonEvent) {
            switch (photonEvent.Code) {
                case PUNEvents.FILL_ROOM:
                    OnFillRoom?.Invoke();
                    break;

                case PUNEvents.BEGIN_LOAD_LEVEL:
                    var data = (object[])photonEvent.CustomData;
                    var sceneIndex = (int)data[0];
                    OnBeginLoadLevel?.Invoke(sceneIndex);
                    break;
            }
        }

        public override void OnJoinRandomFailed(short returnCode, string message) {
            Debug.Log($"PUN: <Color=Red><b>OnJoinRandomFailed</b></color>, creating room");
            CreateRoomMultiplayerMode();
        }

        public override void OnLeftRoom() {
            Debug.Log($"PUN: <Color=Red><b>OnLeftRoom</b></color>");
            PhotonNetwork.SetPlayerCustomProperties(null);
            m_MatchRequested = false;
            OnCancelMatch?.Invoke();
            Utils.LockCursor(false);
        }

        public override void OnJoinedRoom() {
            // ====================================================================
            // This is called on all clients
            // ====================================================================

            Debug.Log($"PUN: <Color=Green><b>OnJoinedRoom ({m_RequestedMode})</b></color>");

            OnPlayerJoined?.Invoke(PhotonNetwork.CurrentRoom.Players);
            
            // Singleplayer start game
            if (PhotonNetwork.CurrentRoom.MaxPlayers == 1) {
                CloseRoom();

                if (SceneManager.GetActiveScene().buildIndex == 0)
                    OnRoomReady?.Invoke();

                return;
            }

            if (m_AutoSetPlayerReady || (string)PhotonNetwork.CurrentRoom.CustomProperties[RoomProps.GAME_MODE] == GameMode.BattleRoyale.ToString())
                SetPlayerReady();

            // Auto fill match
            StartCoroutine(AutoFillMatchRoutine());
        }

        public override void OnMasterClientSwitched(Player newMasterClient) {
            StartCoroutine(AutoFillMatchRoutine());
        }

        protected IEnumerator AutoFillMatchRoutine() {
            var room = PhotonNetwork.CurrentRoom;
            var roomProperties = room.CustomProperties;
            
            if (m_AutoSetPlayerReady || !PhotonNetwork.IsMasterClient)
                yield break;

            if (!roomProperties.TryGetValue(RoomProps.TIMESTAMP, out object roomTimestamp)) {
                roomTimestamp = PhotonNetwork.ServerTimestamp;
                room.SetCustomProperties(new PhotonHashtable { { RoomProps.TIMESTAMP, roomTimestamp } });
            }

            m_RoomStartTime = (int)roomTimestamp;
            m_RoomCurrentTime = m_RoomStartTime;

            var timerDuration = (PhotonNetwork.ServerTimestamp - m_RoomCurrentTime) / 1000;
            var timeout = m_AutoFillMatchTimeout - timerDuration;

            while (room.PlayerCount <= 1)
                yield return null;

            if (timeout <= 0)
                yield break;

            Debug.Log($"<Color=Green><b>Auto fill match in:</b></color> <Color=Orange><b>{timeout}</b></color>");
            yield return new WaitForSecondsRealtime(timeout);

            if (room.MaxPlayers == room.PlayerCount)
                yield break;

            PhotonNetwork.RaiseEvent(PUNEvents.FILL_ROOM, null, m_RaiseEventAll, SendOptions.SendReliable);
        }

        public override void OnPlayerEnteredRoom(Player newPlayer) {
            // ====================================================================
            // This is called on mater client only
            // ====================================================================

            Debug.Log($"PUN: <Color=Green><b>OnPlayerEnteredRoom</b></color>");

            var room = PhotonNetwork.CurrentRoom;
            var roomProps = room.CustomProperties;

            roomProps[$"player{newPlayer.ActorNumber}_ai"] = false;
            roomProps[RoomProps.AI_COUNT] = room.MaxPlayers - room.PlayerCount;

            Debug.Log($"[OnPlayerEnteredRoom] {RoomProps.AI_COUNT}={roomProps[RoomProps.AI_COUNT]}");
            
            if (room.MaxPlayers == room.PlayerCount) {
                StopCoroutine(AutoFillMatchRoutine());
                PhotonNetwork.RaiseEvent(PUNEvents.FILL_ROOM, null, m_RaiseEventAll, SendOptions.SendReliable);
            }

            OnPlayerEnteredRoomEvent?.Invoke(newPlayer);
            m_MatchRequested = false;
        }

        public override void OnPlayerPropertiesUpdate(Player targetPlayer, PhotonHashtable changedProps) {
            Debug.Log($"PUN: <Color=Blue><b>OnPlayerPropertiesUpdate</b></color>");

            var room = PhotonNetwork.CurrentRoom;
            var roomPlayers = room.Players.Values;
            var isReady = changedProps.TryGetValue(m_RequestedMode == GameMode.BattleRoyale ? RoomProps.BR_READY : RoomProps.PLAYER_READY, out object rdy) 
                          && (bool)rdy;

            // Start BR mode
            if (isReady && (string)room.CustomProperties[RoomProps.GAME_MODE] == GameMode.BattleRoyale.ToString()) {
                SelectedMap = ProjectParameters.BattleRoyaleMap.map;
                CloseRoom();
                return;
            }
            else if (isReady) {
                var readyPlayersCount = roomPlayers.Where(p => IsPlayerReady(p)).Count();
                OnPlayerReady?.Invoke(targetPlayer, readyPlayersCount, room.MaxPlayers);
            }

            if (PhotonNetwork.IsMasterClient) {
                var allPlayersReady = room.PlayerCount == room.MaxPlayers && roomPlayers.All(p => IsPlayerReady(p));

                if (allPlayersReady || (CurrentRoomPlayerCount == room.MaxPlayers && roomPlayers.All(p => IsPlayerReady(p)))) {
                    // Start game!
                    CloseRoom();
                    OnRoomReady?.Invoke();
                }
            }
        }

        public static bool IsPlayerReady(Player player) {
            var properties = player.CustomProperties;
            return properties.TryGetValue(RoomProps.PLAYER_READY, out object value) && (bool)value;
        }

        // ============================================================================================================================

        /// <summary>
        /// 
        /// </summary>
        /// <param name="offlineMode"></param>
        /// <param name="onConnect"></param>
        public static void Connect(bool offlineMode, Action onConnect = null) {
            Instance.StopAllCoroutines();

            void connect() {
                Instance.m_OnConnectToMasterInternal = onConnect;
                PhotonNetwork.AutomaticallySyncScene = true;

                if (offlineMode)
                    PhotonNetwork.OfflineMode = true;
                else {
                    // TODO: Connection credentials
                    PhotonNetwork.AuthValues = new AuthenticationValues {
                        UserId = Guid.NewGuid().ToString()
                    };

                    PhotonNetwork.ConnectUsingSettings();
                    PhotonNetwork.GameVersion = Application.version;
                }
            }

            var isConnected = PhotonNetwork.OfflineMode || PhotonNetwork.IsConnected;

            if (PhotonNetwork.OfflineMode)
                PhotonNetwork.OfflineMode = false;

            if (PhotonNetwork.IsConnected)
                PhotonNetwork.Disconnect();

            if (!isConnected)
                connect();
            else
                Instance.Sleep(.1f, connect);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="gameMode"></param>
        public static void PlaySolo(GameMode gameMode = GameMode.PlaySolo) {
            Instance.m_MatchRequested = true;
            Instance.m_RequestedMode = gameMode;

            Connect(true, () => {
                PhotonNetwork.CreateRoom("SinglePlayer", new RoomOptions {
                    MaxPlayers = 1,
                    CustomRoomProperties = new PhotonHashtable() {
                        { "player1_ai", false },
                        { "player1_id", TRPlayer.CharacterId },
                        { "player1_preset", TRPlayer.CharacterPreset.SerializeToJsonBase64() },
                        { RoomProps.AI_COUNT, 0 }
                    }
                });
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="gameMode"></param>
        /// <param name="autoSetPlayerReady"></param>
        public static void FindMatch(GameMode gameMode, bool autoSetPlayerReady = false) {
            if (Instance.m_MatchRequested) {
                Debug.LogWarning("A match has already been requested.");
                return;
            }

            void joinRoom() {
                Instance.m_RequestedMode = gameMode;
                var playersCount = gameMode == GameMode.BattleRoyale ? 0 : (int)gameMode;
                var roomFilter = new PhotonHashtable { { RoomProps.GAME_MODE, gameMode.ToString() } };
                PhotonNetwork.JoinRandomRoom(roomFilter, (byte)(playersCount + playersCount));
            }

            Instance.m_AutoSetPlayerReady = autoSetPlayerReady;
            Instance.m_MatchRequested = true;

            if (!PhotonNetwork.IsConnected || PhotonNetwork.OfflineMode)
                Connect(false, joinRoom);
            else
                joinRoom();
        }

        static void CreateRoomMultiplayerMode() {
            var playersCount = (int)Instance.m_RequestedMode;

            var roomOptions = new RoomOptions {
                MaxPlayers = Instance.m_RequestedMode == GameMode.BattleRoyale ? (byte)0 : (byte)(playersCount + playersCount),
                IsOpen = true,
                IsVisible = true,
                CleanupCacheOnLeave = true,
                DeleteNullProperties = true,
                CustomRoomPropertiesForLobby = new[] { RoomProps.GAME_MODE },
                CustomRoomProperties = GetRoomMultiplayerModeProperties()
            };

            PhotonNetwork.CreateRoom(null, roomOptions, null);
        }

        static PhotonHashtable GetRoomMultiplayerModeProperties() {
            var roomProperties = new PhotonHashtable {
                { RoomProps.GAME_MODE, Instance.m_RequestedMode.ToString() },
                { RoomProps.AI_COUNT, 0 },
                { "player1_ai", false },
                { "player1_id", TRPlayer.CharacterId },
                { "seed", TrickIRandomizer.Default.Next() },
            };

            if (Instance.m_RequestedMode != GameMode.PlaySolo && Instance.m_RequestedMode != GameMode.BattleRoyale)
                for (var i = 1; i < 12; i++) {
                    roomProperties.Add($"player{i + 1}_ai", true);
                    roomProperties.Add($"player{i + 1}_id", -1);
                }

            return roomProperties;
        }

        /// <summary>
        /// Cancel the current match finding process.
        /// </summary>
        public static void CancelMatch() {
            if (PhotonNetwork.IsConnected)
                Instance.StartCoroutine(Instance.CancelMatchInternal());
        }

        IEnumerator CancelMatchInternal() {
            while (!PhotonNetwork.InRoom)
                yield return null;

            PhotonNetwork.LeaveRoom();
        }

        /// <summary>
        /// Mark local player as ready.
        /// </summary>
        public static void SetPlayerReady() {
            var playerProperties = PhotonNetwork.LocalPlayer.CustomProperties;

            if (!PhotonNetwork.InRoom || playerProperties.TryGetValue(RoomProps.PLAYER_READY, out object ready) && (bool)ready) {
                Debug.LogWarning("The local player is already marked as ready.");
                return;
            }

            PhotonNetwork.CurrentRoom.SetCustomProperties(new PhotonHashtable() {
                { $"player{PhotonNetwork.LocalPlayer.ActorNumber}_id", TRPlayer.CharacterId },
                { $"player{PhotonNetwork.LocalPlayer.ActorNumber}_preset", TRPlayer.CharacterPreset.SerializeToJsonBase64() }
            });
            
            Debug.Log($"player{PhotonNetwork.LocalPlayer.ActorNumber} preset: " + TRPlayer.CharacterPreset.SerializeToJson(true, true));

            PhotonNetwork.SetPlayerCustomProperties(new PhotonHashtable() { 
                { Instance.m_RequestedMode == GameMode.BattleRoyale ? RoomProps.BR_READY : RoomProps.PLAYER_READY, true }
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sceneToLoad"></param>
        public static void LoadLevel(SceneReference sceneToLoad) {
            Debug.Log($"<Color=Green><b>Starting game match!</b></color>");

            IEnumerator StartWithDelay()
            {
                yield return new WaitForSeconds(5.0f);
                PhotonNetwork.RaiseEvent(PUNEvents.BEGIN_LOAD_LEVEL, new object[] { sceneToLoad.BuildIndex }, m_RaiseEventAll, SendOptions.SendReliable);
                PhotonNetwork.LoadLevel(sceneToLoad.BuildIndex);
            }

            Instance.StartCoroutine(StartWithDelay());
        }

        static void CloseRoom() {
            if (PhotonNetwork.CurrentRoom.IsOpen)
            {
                Debug.Log($"<Color=Orange><b>Current room closed</b></color>");
                Instance.StopAllCoroutines();
                PhotonNetwork.CurrentRoom.IsOpen = false;
                PhotonNetwork.CurrentRoom.IsVisible = false;

                // FIXME
                LoadLevel(SelectedMap);
            }
        }
    }
}
