using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Animancer;

namespace TriggeRun {

    //[RequireComponent(typeof(name))]
    public class FPSAnimator : MonoBehaviour {

        public AnimationClip idle;
        public AnimationClip run;
        public AnimationClip skillRecharge;
        public AnimationClip skillFire;

        private AnimancerComponent m_Animancer;
        private bool m_TestSwitch;

        void Awake() {
            m_Animancer = GetComponent<AnimancerComponent>();
        }

        void Update() {
            if (/*!m_TestSwitch &&*/ Input.GetMouseButtonDown(1)) {
                //m_TestSwitch = true;
                var a = m_Animancer.Play(skillFire);
                a.Events.OnEnd += () => m_Animancer.Play(idle);
            }

            /*if (m_TestSwitch && Input.GetMouseButtonUp(1)) {
                m_TestSwitch = false;
                m_Animancer.Play(skillFire);
            }*/
        }
    }
}
