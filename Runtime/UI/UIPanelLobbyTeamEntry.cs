using System.Collections.Generic;
using System.Linq;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IEdgeGames
{
    public class UIPanelLobbyTeamEntry : MonoBehaviour
    {
        public TextMeshProUGUI TeamNameText;
        public TextMeshProUGUI TeamScoreText;

        public UIPanelLobbyPlayerEntry PlayerEntryPrefab;
        public RectTransform PlayerEntryContentTransform;

        public HorizontalLayoutGroup TeamNameHorizontalLayoutGroup;

        public List<Image> BackgroundColorImages;
        public List<Color> TeamColors;

        public PlayerTeam CurrentTeamID { get; set; }
        public Dictionary<int, UIPanelLobbyPlayerEntry> m_Entries = new Dictionary<int, UIPanelLobbyPlayerEntry>();
        public void SetTeam(int TeamID)
        {
            CurrentTeamID = (PlayerTeam)TeamID;
            // reverse for the second team the score, so the score is side-to-side with each other 
            TeamNameHorizontalLayoutGroup.reverseArrangement = TeamID % 2 == 1;
            TeamNameText.text = $"{CurrentTeamID} Team";
            TeamScoreText.text = 0.ToString();
            
            BackgroundColorImages.ForEach(image => image.color = TeamColors[TeamID % TeamColors.Count]);
        }

        public void UpdatePlayerEntry(Player player, int playerIndex, bool fill)
        {
            // get or create the entry
            if (!m_Entries.TryGetValue(playerIndex, out var entry))
                m_Entries.Add(playerIndex, entry = Instantiate(PlayerEntryPrefab, PlayerEntryContentTransform));

            entry.SetData(this, player, playerIndex, CurrentTeamID, fill);
            
            // Sort the entries
            m_Entries.Values
                .OrderBy(playerEntry => playerEntry.Index)
                .ToList()
                .ForEach(playerEntry =>
                {
                    playerEntry.transform.SetAsLastSibling();
                });
        }
    }
}