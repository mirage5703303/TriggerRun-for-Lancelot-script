using System.Collections.Generic;
using UnityEngine;

namespace IEdgeGames
{
    public class UIPanelScoreBoard : MonoBehaviour
    {
        public UIPanelScoreBoardTeamEntry scoreBoardTeamEntryPrefab;
        public RectTransform ScoreBoardTeamContentTransform;

        public GameObject Background;
        
        private List<UIPanelScoreBoardTeamEntry> m_scoreBoardTeamEntries = new List<UIPanelScoreBoardTeamEntry>();

        private void TryInitialize()
        {
            if (m_scoreBoardTeamEntries.Count > 0) return;
            
            while (ScoreBoardTeamContentTransform.childCount > 0)
            {
                Transform t = ScoreBoardTeamContentTransform.GetChild(0);
                t.SetParent(null);
                DestroyImmediate(t.gameObject);
            }
            
            // Create the teams, can support later more teams if we want to.
            // Can make a loop later to make it flexiable for more teams
            var team0Entry = Instantiate(scoreBoardTeamEntryPrefab, ScoreBoardTeamContentTransform);
            team0Entry.SetTeam(0);
            m_scoreBoardTeamEntries.Add(team0Entry);
            
            var team1Entry = Instantiate(scoreBoardTeamEntryPrefab, ScoreBoardTeamContentTransform);
            team1Entry.SetTeam(1);
            m_scoreBoardTeamEntries.Add(team1Entry);
        }
        public void UpdateScoreBoard(PlayerScoreData playerScoreData)
        {
            TryInitialize();
            
            var foundEntry = m_scoreBoardTeamEntries.Find(entry => entry.CurrentTeamID == playerScoreData.CurrentPlayerTeam);
            if (foundEntry != null)
            {
                foundEntry.UpdatePlayerEntry(playerScoreData);
            }
        }

        public void UpdateTeamScoreBoard(int team1, int team2)
        {
            TryInitialize();
            
            for (int i = 0; i < m_scoreBoardTeamEntries.Count; i++)
            {
                m_scoreBoardTeamEntries[i].TeamScoreText.text = (i == 0 ? team1 : team2).ToString();
            }
        }
    }
}