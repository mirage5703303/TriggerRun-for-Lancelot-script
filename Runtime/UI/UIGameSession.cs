using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using TMPro;

namespace IEdgeGames {
    public class UIGameSession : UIModule<UIGameSession> {

        // ============================================================================================================================

        [SerializeField] private KeyCode m_ScoreBoardShortcut;
        
        [Header("Panels")]
        [SerializeField] private GameObject m_TiedPanel;
        [SerializeField] private GameObject m_VictoryPanel;
        [SerializeField] private GameObject m_DefeatPanel;
        [SerializeField] private GameObject m_SpectatorPanel;
        [SerializeField] private UIPanelScoreBoard m_ScoreBoardPanel;

        [Header("UI")]
        [SerializeField] private TextMeshProUGUI m_GameModeLabel;
        [SerializeField] private TextMeshProUGUI m_TimeLabel;
        [SerializeField] private TextMeshProUGUI m_ScoreLabel;
        [SerializeField] private Button m_LeaveButton;

        [Header("Colors")]
        [SerializeField] private Color m_GameModeColor = Color.white;
        [SerializeField] private Color m_Team1Color = Color.blue;
        [SerializeField] private Color m_Team2Color = Color.red;

        // ============================================================================================================================

        void Awake() {
            SetupGameModeLabel();
            ShowPanel(MatchStatus.Playing);

            m_ScoreBoardPanel.gameObject.SetActive(false);
            
            m_TimeLabel.text = SessionTimer.MatchDurationString;
            m_LeaveButton.onClick.AddListener(GameSession.LeaveMatch);
        }

        protected override void OnModuleStart() {
            SessionTimer.OnUpdate += UpdateTimer;
            SessionTimer.OnFinish += UpdateTimer;
            GameSession.OnUpdateScore += UpdateScore;
            GameSession.OnEndMatch += ShowPanel;
        }

        protected override void OnModuleDestroy() {
            SessionTimer.OnUpdate -= UpdateTimer;
            SessionTimer.OnFinish -= UpdateTimer;
            GameSession.OnUpdateScore -= UpdateScore;
            GameSession.OnEndMatch -= ShowPanel;
        }

        // ============================================================================================================================

        void UpdateTimer() {
            m_TimeLabel.text = SessionTimer.LeftString;
        }

        void UpdateScore(int team1, int team2) {
            var c1 = ColorUtility.ToHtmlStringRGB(m_Team1Color);
            var c2 = ColorUtility.ToHtmlStringRGB(m_Team2Color);
            m_ScoreLabel.text = $"<#{c1}>{team1}</color> <size=-2>-</size> <#{c2}>{team2}</color>";

            if (m_ScoreBoardPanel != null) m_ScoreBoardPanel.UpdateTeamScoreBoard(team1, team2);
        }

        // ============================================================================================================================

        void ShowPanel(MatchStatus matchStatus) {
            m_TiedPanel.SetActive(matchStatus == MatchStatus.Tied);
            m_VictoryPanel.SetActive(matchStatus == MatchStatus.Victory);
            m_DefeatPanel.SetActive(matchStatus == MatchStatus.Defeat);
            m_SpectatorPanel.SetActive(matchStatus == MatchStatus.Spectator);
            m_LeaveButton.gameObject.SetActive(matchStatus != MatchStatus.Playing);

            if (GameSession.Instance.GameEnded)
            {
                m_ScoreBoardPanel.gameObject.SetActive(true);
                m_ScoreBoardPanel.Background.SetActive(false);
            }
            else
            {
                m_ScoreBoardPanel.Background.SetActive(true);
            }
            
            // lock the cursor on play, and in other menus we need the cursor otherwise we can't click on anything...
            Utils.LockCursor(matchStatus == MatchStatus.Playing);
        }

        void SetupGameModeLabel() {
            var maxPlayers = Application.isPlaying ? PhotonNetwork.CurrentRoom.MaxPlayers : 1;
            var playersPerTeam = maxPlayers == 1 ? 1 : maxPlayers / 2;
            m_GameModeLabel.text = $"{playersPerTeam} <size=-10>VS</size> {playersPerTeam}";
            m_GameModeLabel.color = m_GameModeColor;
        }

        private void Update()
        {
            if (!GameSession.Instance.GameEnded)
            {
                // toggle our scoreboard
                if (Input.GetKeyUp(m_ScoreBoardShortcut))
                {
                    m_ScoreBoardPanel.gameObject.SetActive(false);
                }
                else if (Input.GetKeyDown(m_ScoreBoardShortcut))
                {
                    m_ScoreBoardPanel.gameObject.SetActive(true);
                }
            }
        }

        public void UpdateScoreBoard(PlayerScoreData playerScoreData)
        {
            m_ScoreBoardPanel.UpdateScoreBoard(playerScoreData);
        }

        // ============================================================================================================================

#if UNITY_EDITOR
        void OnValidate() {
            /*if (Application.isPlaying || UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            if (m_GameModeLabel)
                SetupGameModeLabel();

            if (m_ScoreLabel)
                UpdateScore(0, 0);*/
        }
#endif
    }
}
