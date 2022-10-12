using UnityEngine;
using DG.Tweening;
using SensorToolkit;
using Photon.Pun;

namespace IEdgeGames {

    [RequireComponent(typeof(PhotonView))]
    [RequireComponent(typeof(RangeSensor))]
    public class TRDoor : MonoBehaviour {

        [SerializeField] private DOTweenAnimation[] m_Tweens;

        private PhotonView m_PV;
        private bool m_Opened;

        void Awake() {
            if (!PhotonNetwork.IsMasterClient)
                return;

            m_PV = GetComponent<PhotonView>();
            var sensor = GetComponent<RangeSensor>();

            sensor.OnDetected.AddListener((go, s) => OnOpenDoor());
            sensor.OnLostDetection.AddListener((go, s) => OnCloseDoor());
        }

        void OnOpenDoor() {
            if (m_Opened)
                return;

            m_Opened = true;
            m_PV.RPC(nameof(OpenDoor), RpcTarget.All);
        }

        void OnCloseDoor() {
            if (!m_Opened)
                return;

            m_Opened = false;
            m_PV.RPC(nameof(CloseDoor), RpcTarget.All);
        }

        [PunRPC]
        void OpenDoor() {
            foreach (var tween in m_Tweens)
                tween.DOPlayForward();
        }

        [PunRPC]
        void CloseDoor() {
            foreach (var tween in m_Tweens)
                tween.DOPlayBackwards();
        }
    }
}
