using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DG.Tweening;
using Michsky.UI.Shift;

namespace IEdgeGames {

    [RequireComponent(typeof(DOTweenAnimation))]
    public class TogglePanel : MonoBehaviour {

        [SerializeField] private FriendsPanelManager m_FriendsPanel;

        private List<DOTweenAnimation> m_Tweens;
        private bool m_Completed;

        void Awake() {
            m_Tweens = GetComponents<DOTweenAnimation>().ToList();
        }

        /// <summary>
        /// 
        /// </summary>
        public void Toggle() {
            if (m_Completed)
                m_Tweens.ForEach(t => t.DOPlayBackwards());
            else
                m_Tweens.ForEach(t => t.DOPlayForward());

            m_Completed = !m_Completed;
        }

        /// <summary>
        /// 
        /// </summary>
        public void ToggleReset() {
            if (m_Completed) {
                Toggle();
                m_FriendsPanel.AnimateWindow();
            }
        }
    }
}
