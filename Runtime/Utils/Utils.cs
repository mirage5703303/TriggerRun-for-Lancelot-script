using System;
using System.Reflection;
using System.Linq;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

namespace IEdgeGames {

    public static class Utils {

        private static PhotonView m_LocalPlayer;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static string BytesToString(ulong bytes) => bytes <= 1024
                                                           ? Convert.ToInt32(bytes / 1024f) + " kb"
                                                           : Convert.ToInt32(bytes / 1024f / 1024f) + " mb";

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static PhotonView GetLocalPlayer() {
            if (!m_LocalPlayer) {
                var characters = UnityEngine.Object.FindObjectsOfType<CharacterHealthEx>();
                m_LocalPlayer = characters.Select(c => c.GetComponent<PhotonView>()).FirstOrDefault(pv => pv.Owner == PhotonNetwork.LocalPlayer);
            }

            return m_LocalPlayer;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static bool IsLocalPlayerAlive() {
            var localPlayer = GetLocalPlayer();

            if (!localPlayer)
                return false;

            var health = localPlayer.GetComponent<CharacterHealthEx>();
            return health ? health.HealthValue > 0f : false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="localPlayer"></param>
        /// <param name="active"></param>
        public static void SetInputActive(bool active) {
            LockCursor(active);

            var localPlayer = GetLocalPlayer();

            if (localPlayer)
                Opsive.Shared.Events.EventHandler.ExecuteEvent(localPlayer.gameObject, "OnEnableGameplayInput", active);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="locked"></param>
        public static void LockCursor(bool locked) {
            Cursor.lockState = !locked ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = !locked;
        }
    }
}
