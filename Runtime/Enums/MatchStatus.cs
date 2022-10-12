using System;

namespace IEdgeGames {

    [Serializable]
    public enum MatchStatus {
        Playing,
        Tied,
        Victory,
        Defeat,
        Spectator
    }
}
