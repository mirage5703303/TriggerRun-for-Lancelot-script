using System.Linq;
using UnityEngine;
using Opsive.UltimateCharacterController.Traits;
using Opsive.UltimateCharacterController.Traits.Damage;
using Opsive.DeathmatchAIKit;
using Opsive.Shared.Events;
using Photon.Pun;

namespace IEdgeGames {
    
    public class DeathmatchHealthEx : CharacterHealth {

        private PhotonView m_PV;

        protected override void Awake() {
            base.Awake();
            m_PV = GetComponent<PhotonView>();
        }

        /// <summary>
        /// The object has taken been damaged.
        /// </summary>
        /// <param name="damageData">The data associated with the damage.</param>
        public override void OnDamage(DamageData damageData) {
            if (TeamManager.IsTeammate(gameObject, damageData.DamageOriginator.Owner))
                return;

            var attacker = damageData != null && damageData.DamageOriginator != null && damageData.DamageOriginator.Owner
                           ? damageData.DamageOriginator.Owner.GetComponent<CharacterHealthEx>()
                           : null;

            if (attacker) {
                var attribute = attacker.m_AM.Attributes.ToDictionary(x => x.Name);
                var isCritical = attribute["Critical Chance"].Value == -1
                                 ? Random.value < Random.Range(.15f, .26f)
                                 : Random.value < attribute["Critical Chance"].Value;

                // Proccess damage
                damageData.Amount += attribute["Base Damage"].Value;

                // Do crit attack
                if (isCritical)
                    damageData.Amount *= 2f;

                // Show damage
                //attacker.ShowDamage(damageData.Position, damageData.Amount, isCritical);
                //return;
            }

            base.OnDamage(damageData);
        }

        /// <summary>
        /// The character has died. Report the death to interested components.
        /// </summary>
        /// <param name="force">The amount of force which killed the character.</param>
        /// <param name="position">The position of the force.</param>
        /// <param name="attacker">The GameObject that killed the character.</param>
        public override void Die(Vector3 position, Vector3 force, GameObject attacker) {
            TeamManager.CancelBackupRequest(gameObject);
            base.Die(position, force, attacker);

            if (!PhotonNetwork.IsMasterClient)
                return;

            m_PV.RPC(nameof(NotifyDeathRPC), RpcTarget.All, attacker);
        }

        [PunRPC]
        void NotifyDeathRPC(GameObject attacker) {
            var attackerPV = attacker ? attacker.GetComponent<PhotonView>() : null;
            EventHandler.ExecuteEvent("OnPlayerDie", m_PV, attackerPV);
        }
    }
}