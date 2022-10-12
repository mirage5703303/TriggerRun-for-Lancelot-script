using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Opsive.Shared.Input;
using Opsive.UltimateCharacterController.Character;
using Opsive.UltimateCharacterController.Traits;
using Photon.Pun;
using Photon.Pun.UtilityScripts;
using Photon.Realtime;
using ExitGames.Client.Photon;
using Sirenix.Serialization;
using TrickCore;
using UnityEngine.Events;
using EventHandler = Opsive.Shared.Events.EventHandler;
using Random = UnityEngine.Random;

namespace IEdgeGames {
    public class GameSession : MonoBehaviourPunCallbacks {

        // ============================================================================================================================
        
        [SerializeField] private int m_TeamDeathMatchKillTarget = 5;
        [SerializeField] private bool m_TeamDeathMatchAutoFillGameWithAI = false;
        public bool TeamDeathMatchAutoFillGameWithAI => m_TeamDeathMatchAutoFillGameWithAI;
        
        // ============================================================================================================================

        /// <summary>
        /// Singleton instance.
        /// </summary>
        public static GameSession Instance => m_Instance ? m_Instance : (m_Instance = FindObjectOfType<GameSession>());

        /// <summary>
		/// 
		/// </summary>
		public static event System.Action<int, int> OnUpdateScore = delegate { };

        /// <summary>
        /// 
        /// </summary>
		public static event System.Action<MatchStatus> OnEndMatch = delegate { };

        /// <summary>
        /// 
        /// </summary>
		public static event System.Action<PhotonView, PhotonView> OnPlayerDie = delegate { };

        // ============================================================================================================================

        protected static GameSession m_Instance;
        private static Dictionary<PlayerTeam, List<PlayerScoreData>> m_TeamScoreData; //= new Dictionary<PlayerTeam, int> { { PlayerTeam.Blue, 0 }, { PlayerTeam.Red, 0 } };
        private static readonly RaiseEventOptions m_EventAll = new RaiseEventOptions { Receivers = ReceiverGroup.All };

        public bool GameEnded { get; private set; }

        // ============================================================================================================================

        public override void OnEnable() {
            base.OnEnable();

            GameEnded = false;
            
            m_TeamScoreData = new Dictionary<PlayerTeam, List<PlayerScoreData>> {
                { PlayerTeam.Blue, new List<PlayerScoreData>() },
                { PlayerTeam.Red, new List<PlayerScoreData>() }
            };

            if (PhotonNetwork.IsMasterClient)
                SessionTimer.OnFinish += UpdateGameScoresTimeFinishedEvent;

            PhotonNetwork.NetworkingClient.EventReceived += OnEvent;
            EventHandler.RegisterEvent<PhotonView, PhotonView>("OnPlayerDie", OnPlayerDieEvent);
        }

        public override void OnDisable() {
            base.OnDisable();

            if (PhotonNetwork.IsMasterClient)
                SessionTimer.OnFinish -= UpdateGameScoresTimeFinishedEvent;

            PhotonNetwork.NetworkingClient.EventReceived -= OnEvent;
            EventHandler.UnregisterEvent<PhotonView, PhotonView>("OnPlayerDie", OnPlayerDieEvent);
        }

        // ============================================================================================================================

        void OnEvent(EventData photonEvent) {
            switch (photonEvent.Code) {
                case PUNEvents.END_MATCH_FOR_PLAYER:
                    var data = (object[])photonEvent.CustomData;
                    var slaimPlayer = PhotonView.Find((int)data[0]);
                    var isHumanPlayer = (bool)data[1];

                    if (isHumanPlayer && slaimPlayer && PhotonNetwork.LocalPlayer == slaimPlayer.Owner)
                        EndMatch(false);

                    break;

                case PUNEvents.END_MATCH:
                    data = (object[])photonEvent.CustomData;
                    EndMatch((bool)data[0], (PlayerTeam)data[1]);
                    break;
            }
        }

        void EndMatch(bool tiedMatch, PlayerTeam winnerTeam = PlayerTeam.Null)
        {
            GameEnded = true;
            Utils.SetInputActive(false);

            if (tiedMatch) {
                Debug.LogWarning("EndMatch: Tied");

                OnEndMatch?.Invoke(MatchStatus.Tied);
            }
            else if (winnerTeam == PlayerTeam.Null) {
                Debug.LogWarning("EndMatch: Spectator");

                OnEndMatch?.Invoke(MatchStatus.Spectator);
            }
            else
            {
                var localPlayer = Utils.GetLocalPlayer();
                var localPlayerTeam = TeamManagerEx.GetPlayerTeam(localPlayer);
                var victoryOrDefeat = winnerTeam == localPlayerTeam ? MatchStatus.Victory : MatchStatus.Defeat;
                Debug.LogWarning($"EndMatch: {victoryOrDefeat} - Local Team {localPlayerTeam}");

                OnEndMatch?.Invoke(winnerTeam == localPlayerTeam ? MatchStatus.Victory : MatchStatus.Defeat);
            }
        }

        void EndMatchByTimer() {
            //var teams = TeamManagerEx.Teams;
            var t1Score = m_TeamScoreData[PlayerTeam.Blue].Sum(data => data.Kills);
            var t2Score = m_TeamScoreData[PlayerTeam.Red].Sum(data => data.Kills);
            var tiedMatch = false;
            var winnerTeam = PlayerTeam.Null;

            // t1 wins
            if (t1Score > t2Score) {
                winnerTeam = PlayerTeam.Blue;
            }

            // t2 wins
            else if (t2Score > t1Score) {
                winnerTeam = PlayerTeam.Red;
            }

            // equalized score, determine victory for team with more shield/health amount
            else if (t1Score == t2Score) {
                var t1TotalHealth = GetAlivePlayersOfTeam(PlayerTeam.Blue).Select(p => p.ShieldValue + p.HealthValue).Sum();
                var t2TotalHealth = GetAlivePlayersOfTeam(PlayerTeam.Red).Select(p => p.ShieldValue + p.HealthValue).Sum();

                /*var alivePlayersBlue = GetAlivePlayersOfTeam(PlayerTeam.Blue);
                var alivePlayersRed = GetAlivePlayersOfTeam(PlayerTeam.Red);
                var humanPlayersBlueCount = alivePlayersBlue.Select(p => p.GetComponent<CharacterHealthEx>()).Count();
                var humanPlayersRedCount = alivePlayersRed.Select(p => p.GetComponent<CharacterHealthEx>()).Count();
                float t1TotalHealth;
                float t2TotalHealth;

                if (humanPlayersBlueCount == humanPlayersRedCount) {
                    t1TotalHealth = alivePlayersBlue.Select(p => p.ShieldValue + p.HealthValue).Sum();
                    t2TotalHealth = alivePlayersRed.Select(p => p.ShieldValue + p.HealthValue).Sum();
                }
                else {
                    t1TotalHealth = alivePlayersBlue.Select(p => p.HealthValue).Sum();
                    t2TotalHealth = alivePlayersRed.Select(p => p.HealthValue).Sum();
                }*/

                // t1 wins
                if (t1TotalHealth > t2TotalHealth) {
                    winnerTeam = PlayerTeam.Blue;
                }

                // t2 wins
                else if (t2TotalHealth > t1TotalHealth) {
                    winnerTeam = PlayerTeam.Blue;
                }

                // tied match!!
                else {
                    tiedMatch = true;
                }
            }

            PhotonNetwork.RaiseEvent(PUNEvents.END_MATCH, new object[] { tiedMatch, winnerTeam }, m_EventAll, SendOptions.SendReliable);
        }

        /// <summary>
        /// A function redirecting to the UpdateGameScores, where we handle if a team wins or not
        /// </summary>
        void UpdateGameScoresTimeFinishedEvent() => UpdateGameScores(null);
        
        void UpdateGameScores(PhotonView slainPlayer)
        {
            // score update by game mode type
            if (UISelectMode.IsTeamDeathMatch)
            {
                var blueScore = m_TeamScoreData[PlayerTeam.Blue].Sum(data => data.Kills);
                var redScore = m_TeamScoreData[PlayerTeam.Red].Sum(data => data.Kills);
                
                // if null, it came from the finished, otherwise from the OnPlayerDieEvent
                if (slainPlayer == null)
                {
                    // Time finished, the winning team is the team with the highest score.
                    // TODO: if blue and red score are equal after time finished, we can do overtime?
                    if (blueScore == redScore)
                        PhotonNetwork.RaiseEvent(PUNEvents.END_MATCH, new object[] { true, PlayerTeam.Null }, m_EventAll, SendOptions.SendReliable);
                    else if (blueScore > redScore)
                        PhotonNetwork.RaiseEvent(PUNEvents.END_MATCH, new object[] { false, PlayerTeam.Blue }, m_EventAll, SendOptions.SendReliable);
                    else if (redScore > blueScore)
                        PhotonNetwork.RaiseEvent(PUNEvents.END_MATCH, new object[] { false, PlayerTeam.Red }, m_EventAll, SendOptions.SendReliable);
                }
                else if (blueScore >= m_TeamDeathMatchKillTarget)
                {
                    // blue team wins 
                    PhotonNetwork.RaiseEvent(PUNEvents.END_MATCH, new object[] { false, PlayerTeam.Blue }, m_EventAll, SendOptions.SendReliable);
                }
                else if (redScore >= m_TeamDeathMatchKillTarget)
                {
                    // red team wins
                    PhotonNetwork.RaiseEvent(PUNEvents.END_MATCH, new object[] { false, PlayerTeam.Red }, m_EventAll, SendOptions.SendReliable);
                }
            }
            else
            {
                // Handle the old end match style
                if (slainPlayer != null) EndMatchByScore(slainPlayer);
            }
        }

        void EndMatchByScore(PhotonView slaimPlayer) {
            //var alivePlayersCount = FindObjectsOfType<CharacterHealth>().Where(p => p.HealthValue > 0f).Count();
            var blueTeamAlivePlayersCount = GetAlivePlayersOfTeam(PlayerTeam.Blue).Count;
            var redTeamAlivePlayersCount = GetAlivePlayersOfTeam(PlayerTeam.Red).Count;
            var t1Score = m_TeamScoreData[PlayerTeam.Blue].Sum(data => data.Kills);
            var t2Score = m_TeamScoreData[PlayerTeam.Red].Sum(data => data.Kills);

            //Debug.LogWarning(alivePlayersCount);

            // match ends only if we have 1 or none players alive
            //if (alivePlayersCount <= 1) {
            if (blueTeamAlivePlayersCount <= 0 || redTeamAlivePlayersCount <= 0) {
                // tied match, calculate score by stop the timer
                if (t1Score == t2Score) {
                    SessionTimer.Stop();
                }

                // otherwise, team with highest score wins
                else {
                    // t1 wins?
                    if (t1Score > t2Score) {
                        SessionTimer.Stop(false);
                        PhotonNetwork.RaiseEvent(PUNEvents.END_MATCH, new object[] { false, PlayerTeam.Blue }, m_EventAll, SendOptions.SendReliable);
                    }

                    // t2 wins?
                    else if (t2Score > t1Score) {
                        SessionTimer.Stop(false);
                        PhotonNetwork.RaiseEvent(PUNEvents.END_MATCH, new object[] { false, PlayerTeam.Red }, m_EventAll, SendOptions.SendReliable);
                    }
                }
            }

            // finish match for killed player
            else {
                var isHumanPlayer = slaimPlayer.GetComponent<CharacterHealthEx>() != null;
                PhotonNetwork.RaiseEvent(PUNEvents.END_MATCH_FOR_PLAYER, new object[] { slaimPlayer.ViewID, isHumanPlayer }, m_EventAll, SendOptions.SendReliable);
            }
        }

        /// <summary>
        /// Calculate score for opposite team of the killed player, killer data is not taken into account.
        /// </summary>
        /// <param name="killedData"></param>
        /// <param name="killerData"></param>
        void OnPlayerDieEvent(PhotonView slainPlayer, PhotonView killerPlayer)
        {
            // Make sure the game is not already ended
            if (GameEnded) return;
            
            //var teams = TeamManagerEx.Teams;
            Debug.LogWarning($"[OnPlayerDieEvent] slainPlayer team {TeamManagerEx.GetPlayerTeam(slainPlayer)}");
            Debug.LogWarning($"[OnPlayerDieEvent] killerPlayer team {TeamManagerEx.GetPlayerTeam(killerPlayer)}");
            switch (TeamManagerEx.GetPlayerTeam(slainPlayer))
            {
                case PlayerTeam.Null:
                    break;
                case PlayerTeam.Blue:
                    m_TeamScoreData[PlayerTeam.Blue].Find(data => data.PlayerReference.photonView.ViewID == slainPlayer.ViewID)?.AddDeath();
                    m_TeamScoreData[PlayerTeam.Red].Find(data => data.PlayerReference.photonView.ViewID == killerPlayer.ViewID)?.AddKill();
                    break;
                case PlayerTeam.Red:
                    m_TeamScoreData[PlayerTeam.Red].Find(data => data.PlayerReference.photonView.ViewID == slainPlayer.ViewID)?.AddDeath();
                    m_TeamScoreData[PlayerTeam.Blue].Find(data => data.PlayerReference.photonView.ViewID == killerPlayer.ViewID)?.AddKill();
                    break;
            }
            
            OnUpdateScore?.Invoke(m_TeamScoreData[PlayerTeam.Blue].Sum(data => data.Kills), m_TeamScoreData[PlayerTeam.Red].Sum(data => data.Kills));
            OnPlayerDie?.Invoke(slainPlayer, killerPlayer);

            if (PhotonNetwork.IsMasterClient)
            {
                UpdateGameScores(slainPlayer);
            }

            if (slainPlayer != null)
            {
                var temp = slainPlayer;
                IEnumerator DelayRespawn()
                {
                    var respawner = temp.GetComponent<Respawner>();
                    yield return new WaitForSeconds(respawner.MaxRespawnTime);
                    respawner.Respawn();
                    Debug.Log("[OnPlayerDieEvent] respawn: " + temp);
                }
                StartCoroutine(DelayRespawn());
                Debug.Log("[OnPlayerDieEvent] Schedule respawn: " + temp);
            }

        }

        // ============================================================================================================================

        /// <summary>
        /// 
        /// </summary>
        public static void LeaveMatch() {
            UIManager.Instance.ShowOnly<LoadingMenu>().WaitForSceneLoad(null, 0, () =>
            {
                UIManager.Instance.ShowOnly<MainMenu>().FadeIn();
            });
            PhotonNetwork.LeaveRoom();
        }

        public override void OnLeftRoom() {
            //DisableLocalPlayerInput();
            Utils.SetInputActive(false);
            PhotonNetwork.SetPlayerCustomProperties(null);
            
            UIManager.Instance.ShowOnly<LoadingMenu>().WaitForSceneLoad(null, 0, () =>
            {
                UIManager.Instance.ShowOnly<MainMenu>().FadeIn();
            });
            SceneManager.LoadScene(0);
        }

        public override void OnDisconnected(DisconnectCause cause) {
            OnLeftRoom();
        }

        // ============================================================================================================================

        /*static void DisableLocalPlayerInput() {
            try {
                var playerInput = FindObjectsOfType<UnityInput>().Where(c => c.GetComponent<PhotonView>().Owner == PhotonNetwork.LocalPlayer).FirstOrDefault();
                playerInput.DisableCursor = false;
                EventHandler.ExecuteEvent(playerInput.gameObject, "OnEnableGameplayInput", false);
            }
            catch { }
        }*/

        static PlayerTeam GetOppositeTeam(PhotonView player) {
            //var teams = TeamManagerEx.Teams;
            var playerTeam = TeamManagerEx.GetPlayerTeam(player);
            return playerTeam == PlayerTeam.Blue ? PlayerTeam.Red : PlayerTeam.Blue;//teams.FirstOrDefault(t => t != playerTeam);
        }

        static List<CharacterHealth> GetAlivePlayersOfTeam(PlayerTeam team) {
            var players = FindObjectsOfType<CharacterHealth>().Where(p => p.HealthValue > 0f);
            var result = new List<CharacterHealth>();

            foreach (var p in players) {
                var pv = p.GetComponent<PhotonView>();

                if (!pv)
                    continue;

                if (TeamManagerEx.GetPlayerTeam(pv) == team)
                    result.Add(p);
            }

            return result;
        }

        /// <summary>
        /// Called when we instantiate a player
        /// </summary>
        /// <param name="player"></param>
        /// <param name="playerIndex"></param>
        public void RegisterPlayer(TRCharacter player, int playerIndex, int teamID, bool isAI)
        {
            player.PlayerIndex = playerIndex;
            player.IsAI = isAI;
            player.Team = (PlayerTeam)teamID;
            var scoreData = new PlayerScoreData(player, (PlayerTeam)teamID);
            
            
            if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue($"player{playerIndex}_preset", out object presetObj) && 
                presetObj is string presetBase64 && 
                presetBase64.DeserializeJsonBase64<TriggeRunPresetData>() is {} preset)
            {
                // give the load to the player
                player.SetPreset(preset);
                Debug.Log($"Apply preset to player: {playerIndex} (ai={isAI})");
            }
            else
            {
                Debug.Log($"No preset set to apply for player: {playerIndex} (ai={isAI})");
                player.SetPreset(TriggeRunGameManager.Instance.AIPreset);
            }

            
            
            m_TeamScoreData[scoreData.CurrentPlayerTeam].Add(scoreData);
            void ScoreUpdate(PlayerScoreData data) => UIGameSession.Instance.UpdateScoreBoard(data);
            scoreData.OnUpdateScore += ScoreUpdate;
            
            UIGameSession.Instance.UpdateScoreBoard(scoreData);
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (GameEnded)
            {
                IEnumerator ExecNextFrame()
                {
                    yield return null;
                    Utils.SetInputActive(false);
                }
                StartCoroutine(ExecNextFrame());
            }
        }
    }

    public class PlayerScoreData
    {
        public PlayerScoreData(TRCharacter player, PlayerTeam team)
        {
            PlayerReference = player;
            CurrentPlayerTeam = team;
        }

        public PlayerTeam CurrentPlayerTeam { get; }

        public TRCharacter PlayerReference { get; }

        public int Kills { get; private set; }
        public int Deaths { get; private set; }

        public event System.Action<PlayerScoreData> OnUpdateScore = delegate { };

        
        public void AddDeath()
        {
            Deaths++;

            OnUpdateScore?.Invoke(this);
        }

        public void AddKill()
        {
            Kills++;

            OnUpdateScore?.Invoke(this);
        }
    }
}
