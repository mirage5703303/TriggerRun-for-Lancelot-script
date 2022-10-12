using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IEdgeGames
{
    public class UIPanelScoreBoardTeamEntry : MonoBehaviour
    {
        public TextMeshProUGUI TeamNameText;
        public TextMeshProUGUI TeamScoreText;

        public UIPanelScoreBoardPlayerEntry PlayerEntryPrefab;
        public RectTransform PlayerEntryContentTransform;

        public HorizontalLayoutGroup TeamNameHorizontalLayoutGroup;

        public List<Image> BackgroundColorImages;
        public List<Color> TeamColors;

        public PlayerTeam CurrentTeamID { get; set; }
        public Dictionary<int, UIPanelScoreBoardPlayerEntry> m_Entries = new Dictionary<int, UIPanelScoreBoardPlayerEntry>();
        public void SetTeam(int TeamID)
        {
            CurrentTeamID = (PlayerTeam)TeamID;
            // reverse for the second team the score, so the score is side-to-side with each other 
            TeamNameHorizontalLayoutGroup.reverseArrangement = TeamID % 2 == 1;
            TeamNameText.text = $"{CurrentTeamID} Team";
            TeamScoreText.text = 0.ToString();
            
            BackgroundColorImages.ForEach(image => image.color = TeamColors[TeamID % TeamColors.Count]);
        }

        public void UpdatePlayerEntry(PlayerScoreData playerScoreData)
        {
            // get or create the entry
            if (!m_Entries.TryGetValue(playerScoreData.PlayerReference.photonView.ViewID, out var entry))
                m_Entries.Add(playerScoreData.PlayerReference.photonView.ViewID, entry = Instantiate(PlayerEntryPrefab, PlayerEntryContentTransform));

            entry.SetData(this, playerScoreData);
            
            // Sort the entries
            m_Entries.Values
                .OrderByDescending(playerEntry => playerEntry.CurrentPlayerScoreData.Kills)
                .ThenBy(playerEntry => playerEntry.CurrentPlayerScoreData.Deaths)
                .ThenBy(playerEntry => playerEntry.CurrentPlayerScoreData.PlayerReference.PlayerIndex)
                .ToList()
                .ForEach(playerEntry =>
                {
                    playerEntry.transform.SetAsLastSibling();
                });
        }
    }
}