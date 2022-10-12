using System.Linq;
using TrickCore;
using UnityEngine;

namespace IEdgeGames {

    public static class TRPlayer {

        // ============================================================================================================================

        const string CHARACTER_ID_KEY = "tr_player_char_id";

        // ============================================================================================================================

        /// <summary>
        /// 
        /// </summary>
        public static CharacterDefinition Character => CharacterContent.Characters.FirstOrDefault(c => c.id == CharacterId);

        /// <summary>
        /// 
        /// </summary>
        public static int CharacterId => TriggeRunGameManager.Instance.CurrentPlayerData.GetPreset().HeroId;
        public static TriggeRunPresetData CharacterPreset => TriggeRunGameManager.Instance.CurrentPlayerData.GetPreset();
    }
}
