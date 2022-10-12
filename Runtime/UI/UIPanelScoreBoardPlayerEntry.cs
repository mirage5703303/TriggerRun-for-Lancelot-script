using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IEdgeGames
{
    public class UIPanelScoreBoardPlayerEntry : MonoBehaviour
    {
        public TextMeshProUGUI PlayerNameText;
        public TextMeshProUGUI KillText;
        public TextMeshProUGUI DeathText;

        public Image Background;
        
        public void SetData(UIPanelScoreBoardTeamEntry teamEntry, PlayerScoreData playerScoreData)
        {
            CurrentPlayerScoreData = playerScoreData;
            Background.color = teamEntry.TeamColors[(int)playerScoreData.CurrentPlayerTeam];
            PlayerNameText.text = (playerScoreData.PlayerReference.IsAI ? "AI #" : "Player #") + $"{playerScoreData.PlayerReference.PlayerIndex}";
            KillText.text = playerScoreData.Kills.ToString();
            DeathText.text = playerScoreData.Deaths.ToString();
        }

        public PlayerScoreData CurrentPlayerScoreData { get; set; }
    }
}