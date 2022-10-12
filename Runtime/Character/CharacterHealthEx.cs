using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Opsive.Shared.Events;
using Opsive.UltimateCharacterController.Traits;
using Opsive.UltimateCharacterController.Traits.Damage;
using Photon.Pun;
using Photon.Pun.UtilityScripts;
using Photon.Realtime;
using Opsive.DeathmatchAIKit;

namespace IEdgeGames {
    public class CharacterHealthEx : CharacterHealth {

        //internal PhotonTeam MyTeam => m_MyTeam ??= m_PV.Owner.GetPhotonTeam();
        //internal PhotonTeam m_MyTeam;
        internal PhotonView m_PV;
        internal AttributeManager m_AM;

        protected override void Awake() {
            base.Awake();
            m_PV = GetComponent<PhotonView>();
            m_AM = GetComponent<AttributeManager>();
            Debug.Log("[CharacterHealthEx] Awake");
        }

        void OnEnable() {
            m_PV = GetComponent<PhotonView>();
            m_AM = GetComponent<AttributeManager>();
            Debug.Log("[CharacterHealthEx] OnEnable");
            EventHandler.RegisterEvent<Dictionary<string, bool>, GameObject, GameObject>(gameObject, nameof(OnBulletHit), OnBulletHit);
        }

        void OnDisable() {
            EventHandler.UnregisterEvent<Dictionary<string, bool>, GameObject, GameObject>(gameObject, nameof(OnBulletHit), OnBulletHit);
        }

        void OnBulletHit(Dictionary<string, bool> hitProp, GameObject source, GameObject target) {
            if (source != gameObject/* || MyTeam == null*/)
                return;

            //var targetHealth = target.GetComponent<CharacterHealthEx>();

            if (TeamManager.IsTeammate(gameObject, target)/*targetHealth && targetHealth.MyTeam == MyTeam*/)
                hitProp["canHit"] = false;
        }

        public override void Damage(DamageData damageData) {
            /*if (MyTeam != null && damageData != null && damageData.DamageOriginator != null && damageData.DamageOriginator.Owner) {
                var attacker = damageData.DamageOriginator.Owner.GetComponent<CharacterHealthEx>();

                if (attacker && attacker.MyTeam == MyTeam)
                    return;
            }

            var attribute = m_AM.Attributes.ToDictionary(x => x.Name);

            // Proccess damage
            if (attribute.ContainsKey("Base Damage"))
                damageData.Amount += attribute["Base Damage"].Value;

            // Do crit attack
            if (attribute.ContainsKey("Critical Chance") && Random.value < attribute["Critical Chance"].Value)
                damageData.Amount *= 2f;

            base.Damage(damageData);*/


            if (damageData == null
                || damageData.DamageOriginator == null
                || !damageData.DamageOriginator.Owner
                || TeamManager.IsTeammate(gameObject, damageData.DamageOriginator.Owner)
            )
                return;

            if (m_AM == null)
            {
                m_AM = GetComponent<AttributeManager>();
                Debug.Log("m_AM is null, trying to find it: " + m_AM);
            }
            
            if (m_AM != null && m_AM.Attributes != null)
            {
                var attribute = m_AM.Attributes.ToDictionary(x => x.Name);
                var isCritical = attribute["Critical Chance"].Value == -1
                    ? Random.value < Random.Range(.15f, .26f)
                    : Random.value < attribute["Critical Chance"].Value;

                // Proccess damage
                damageData.Amount += attribute["Base Damage"].Value;

                // Do crit attack
                if (isCritical)
                    damageData.Amount *= 2f;
            }

            base.Damage(damageData);
        }

        /// <summary>
        /// Die without notify to score system.
        /// </summary>
        public void Die() {
            base.Die(transform.position, Vector3.zero, null);
        }

        public override void Die(Vector3 position, Vector3 force, GameObject attacker) {
            base.Die(position, force, attacker);

            if (!PhotonNetwork.IsMasterClient)
                return;


            if (m_PV == null)
            {
                m_PV = GetComponent<PhotonView>();
                Debug.Log("[CharacterHealthEx] Find PV: " + m_PV);
            }
            
            var view = attacker.GetComponent<PhotonView>();
            m_PV.RPC(nameof(NotifyDeathRPC), RpcTarget.All, view != null ? view.ViewID : 0);
        }

        [PunRPC]
        void NotifyDeathRPC(int viewID) {
            var attackerPV = viewID != 0 ? PhotonNetwork.GetPhotonView(viewID) : null;
            
            if (m_PV == null)
            {
                m_PV = GetComponent<PhotonView>();
                Debug.Log("[CharacterHealthEx] Find PV: " + m_PV);
            }
            
            EventHandler.ExecuteEvent("OnPlayerDie", m_PV, attackerPV != null ? attackerPV : null);
        }
    }
}
