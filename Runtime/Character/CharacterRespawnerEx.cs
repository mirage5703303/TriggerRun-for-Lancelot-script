using Opsive.UltimateCharacterController.Traits;
using Photon.Pun;

namespace IEdgeGames {

    public class CharacterRespawnerEx : CharacterRespawner {

        private PhotonView m_PhotonView;

        protected override void Awake() {
            base.Awake();
            m_PhotonView = GetComponent<PhotonView>();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="groupId"></param>
        public void SetSpawnGroupId(int groupId) {
            m_PhotonView.RPC(nameof(SetSpawnGroupIdRPC), RpcTarget.All, groupId);
        }

        [PunRPC]
        void SetSpawnGroupIdRPC(int groupId) {
            m_Grouping = groupId;
        }
    }
}
