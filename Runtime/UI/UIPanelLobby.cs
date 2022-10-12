using System;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace IEdgeGames
{
    public class UIPanelLobby : MonoBehaviour
    {
        public UIPanelLobbyTeamEntry LobbyTeamEntryPrefab;
        public RectTransform LobbyTeamContentTransform;

        public GameObject Background;
        
        private List<UIPanelLobbyTeamEntry> m_lobbyTeamEntries = new List<UIPanelLobbyTeamEntry>();

        private void TryInitialize()
        {
            if (m_lobbyTeamEntries.Count > 0) return;
            // Create the teams, can support later more teams if we want to.
            // Can make a loop later to make it flexiable for more teams
            var team0Entry = Instantiate(LobbyTeamEntryPrefab, LobbyTeamContentTransform);
            team0Entry.SetTeam(0);
            m_lobbyTeamEntries.Add(team0Entry);
            
            var team1Entry = Instantiate(LobbyTeamEntryPrefab, LobbyTeamContentTransform);
            team1Entry.SetTeam(1);
            m_lobbyTeamEntries.Add(team1Entry);
        }
        
        public void UpdateLobby(Dictionary<int, Player> obj, bool fill)
        {
            TryInitialize();

            Debug.Log($"[UpdateLobby] fill: {fill} (players:{obj.Count})");
            
            foreach (KeyValuePair<int, Player> pair in obj)
            {
                var team = (PlayerTeam)(pair.Value.ActorNumber % 2);
                
                var foundEntry = m_lobbyTeamEntries.Find(entry => entry.CurrentTeamID == team);
                if (foundEntry != null)
                {
                    foundEntry.UpdatePlayerEntry(pair.Value, pair.Value.ActorNumber, fill);
                }
            }

            int startIndex = obj.Count + 1;
            int maxPlayers = GetMaxPlayerByGameMode();
            for (int i = startIndex; i <= maxPlayers; i++)
            {
                var team = (PlayerTeam)(i % 2);
                
                var foundEntry = m_lobbyTeamEntries.Find(entry => entry.CurrentTeamID == team);
                if (foundEntry != null)
                {
                    foundEntry.UpdatePlayerEntry(null, i, fill);
                }
            }
        }

        public int GetMaxPlayerByGameMode()
        {
            // if we are in a room, set max players to the game mode
            if (PhotonNetwork.CurrentRoom != null)
                return PhotonNetwork.CurrentRoom.MaxPlayers;

            switch (UISelectMode.SelectedMode)
            {
                case GameMode.Random:
                    break;
                case GameMode.VS1:
                    return 2;
                case GameMode.VS2:
                    return 4;
                case GameMode.VS3:
                    return 6;
                case GameMode.VS4:
                    return 8;
                case GameMode.VS5:
                    return 10;
                case GameMode.VS6:
                    return 12;
                case GameMode.PlaySolo:
                    return 1;
                case GameMode.BattleRoyale:
                    break;
            }

            return 0;
        }
    }
}