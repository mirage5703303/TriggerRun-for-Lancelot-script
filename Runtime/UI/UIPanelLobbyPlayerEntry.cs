using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IEdgeGames
{
    public class UIPanelLobbyPlayerEntry : MonoBehaviour
    {
        public TextMeshProUGUI PlayerNameText;

        public Image Background;
        public Image Ready;
        public void SetData(UIPanelLobbyTeamEntry teamEntry, Player player, int playerIndex, PlayerTeam currentTeamID, bool fill)
        {
            CurrentPlayer = player;
            Background.color = teamEntry.TeamColors[(int)currentTeamID];
            PlayerNameText.text = (player == null ? (fill ? $"AI #{playerIndex}" : string.Empty) : $"Player #{playerIndex}");
            Index = playerIndex;

            Ready.gameObject.SetActive(fill || player != null && Matchmaking.IsPlayerReady(player));
        }

        public int Index { get; set; }

        public Player CurrentPlayer { get; set; }
    }
}