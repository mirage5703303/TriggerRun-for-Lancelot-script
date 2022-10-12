using System;
using System.Collections.Generic;
using UnityEngine;
using Beamable;
using Beamable.Common.Api.Leaderboards;
using Beamable.Common.Leaderboards;
using Beamable.Player;
using Beamable.Leaderboards;

namespace IEdgeGames {

    public class UILeaderboar : MonoBehaviour {

        [SerializeField] private LeaderboardRef m_LeaderboardRef;

        private LeaderBoardView m_View;

        async void Start() {
            var beamable = await API.Instance;
            m_View = await beamable.LeaderboardService.GetBoard(m_LeaderboardRef, 0, 50, focus: beamable.User.id);

			//Debug.LogWarning(m_View.rankings.Count);

			foreach (var rank in m_View.rankings) {
				// need to load alias, and character stats
				//var stats = await beamable.StatsService.GetStats("client", "public", "player", rank.gt);


				/*Debug.LogWarning(rank.gt);
				stats.TryGetValue("alias", out string alias);
				Debug.LogWarning(alias);*/

				/*var character = await PlayerInventory.GetSelectedCharacter(rank.gt);
				var icon = await character.icon.LoadSprite();

				if (stats.TryGetValue("alias", out string alias) && string.IsNullOrEmpty(alias))
					alias = "Anonymous";

				var instance = Instantiate(EntryPrefab, RankContainer);
				instance.Set(alias, icon, rank);

				if (rank.gt == beamable.User.id)
					instance.SetForSelf();*/
			}


            SetupBeamable();
        }

        BeamContext _beamContext;

        private async void SetupBeamable() {
            _beamContext = BeamContext.Default;
            await _beamContext.OnReady;

            Debug.Log($"_beamContext.PlayerId = {_beamContext.PlayerId}");

            var leaderboardContent = await m_LeaderboardRef.Resolve();

            Debug.Log($"PopulateLeaderboard Starting. Wait < 30 seconds... ");

            var leaderboardRowCountMin = 10;
            var leaderboardScoreMin = 99;
            var leaderboardScoreMax = 99999;

            // Populate with custom values 
            var leaderboardStats = new Dictionary<string, object> {
                { "leaderboard_score_timestamp", new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds() },
                { "leaderboard_score_velocity", 99 }, // 99 score delta per second
                { "leaderboard_score_premium_user", 99 }
            };

            // Populates mock "alias" and "score" for each leaderboard row
            var loggingResult = await MockDataCreator.PopulateLeaderboardWithMockData(
                _beamContext,
                leaderboardContent,
                leaderboardRowCountMin,
                leaderboardScoreMin,
                leaderboardScoreMax,
                leaderboardStats
            );

            Debug.Log($"PopulateLeaderboard Finish. Result = {loggingResult}");
        }
    }
}
