using System;
using System.Collections;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

namespace IEdgeGames {
	
	public class SessionTimer : SingletonBehaviour<SessionTimer> {

		// ============================================================================================================================

		[SerializeField] private double m_MatchDuration = 5;

		// ============================================================================================================================

		/// <summary>
		/// 
		/// </summary>
		public static event Action OnUpdate = delegate { };

		/// <summary>
		/// 
		/// </summary>
		public static event Action OnFinish = delegate { };

		/// <summary>
		/// 
		/// </summary>
		public static TimeSpan MatchDuration => TimeSpan.FromMinutes(Instance.m_MatchDuration);

		/// <summary>
		/// 
		/// </summary>
		public static string MatchDurationString => MatchDuration.ToString(@"mm\:ss");

		/// <summary>
		/// 
		/// </summary>
		public static TimeSpan Left => m_StartTime;

		/// <summary>
		/// 
		/// </summary>
		public static string LeftString => m_StartTime != default && m_StartTime.TotalSeconds >= 0f ? m_StartTime.ToString(@"mm\:ss") : "00:00";

		// ============================================================================================================================

		private static TimeSpan m_StartTime;
		private static readonly RaiseEventOptions m_EventMasterClient = new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient };
		private static readonly RaiseEventOptions m_EventAll = new RaiseEventOptions { Receivers = ReceiverGroup.All };
		private static readonly RaiseEventOptions m_EventOthers = new RaiseEventOptions();

		// ============================================================================================================================

		protected override void SingletonStart() {
			if (PhotonNetwork.IsMasterClient) {
				m_StartTime = TimeSpan.FromMinutes(m_MatchDuration);
				StartCoroutine(UpdateTimer());
			}
			else
				PhotonNetwork.RaiseEvent(PUNEvents.TIMER_REQUEST, PhotonNetwork.LocalPlayer, m_EventMasterClient, SendOptions.SendReliable);
		}

		void OnEnable() {
			PhotonNetwork.NetworkingClient.EventReceived += OnEvent;
		}

		void OnDisable() {
			m_StartTime = default;
			PhotonNetwork.NetworkingClient.EventReceived -= OnEvent;
		}

		void OnEvent(EventData photonEvent) {
			switch (photonEvent.Code) {
				case PUNEvents.TIMER_REQUEST:
					if (PhotonNetwork.IsMasterClient) {
						var requestPlayer = photonEvent.CustomData as Player;
						var sendData = new object[] { requestPlayer, m_StartTime.TotalSeconds };
						PhotonNetwork.RaiseEvent(PUNEvents.SYNC_TIMER, sendData, m_EventOthers, SendOptions.SendReliable);
					}
					break;

				case PUNEvents.SYNC_TIMER:
					var data = (object[])photonEvent.CustomData;

					if (!PhotonNetwork.IsMasterClient && (Player)data[0] == PhotonNetwork.LocalPlayer) {
						m_StartTime = TimeSpan.FromSeconds((double)data[1]);
						StartCoroutine(UpdateTimer());
					}
					break;

				case PUNEvents.FINISH_TIMER:
					var inokeEvent = (bool)photonEvent.CustomData;

					StopAllCoroutines();

					if (inokeEvent)
						OnFinish?.Invoke();
					break;
			}
		}

		IEnumerator UpdateTimer() {
			while (m_StartTime != default && m_StartTime.TotalSeconds > 0f) {
				yield return new WaitForSeconds(1f);

				m_StartTime = m_StartTime.Subtract(TimeSpan.FromSeconds(1));

				if (m_StartTime.TotalSeconds > 0f)
					OnUpdate?.Invoke();
			}

			OnUpdate?.Invoke();
			Stop();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="invokeEvent"></param>
		public static void Stop(bool invokeEvent = true) {
			if (PhotonNetwork.IsMasterClient)
				PhotonNetwork.RaiseEvent(PUNEvents.FINISH_TIMER, invokeEvent, m_EventAll, SendOptions.SendReliable);
		}
    }
}
