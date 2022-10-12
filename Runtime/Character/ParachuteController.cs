using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Opsive.UltimateCharacterController.Character.Abilities;
using Opsive.Shared.Input;
using Opsive.UltimateCharacterController.Camera;
using Opsive.UltimateCharacterController.Character;
using Animancer;

namespace IEdgeGames {

    [RequireComponent(typeof(NamedAnimancerComponent))]
    public class ParachuteController : MonoBehaviour {

        [SerializeField] private AnimationClip m_ParachuteIdle;
        [SerializeField] private LayerMask m_RaycastMask;
        [SerializeField] private float m_ClothRemoveDelay = 2f;
        [SerializeField] private float m_MinOpenDistance = 150f;
        [SerializeField] private float m_MinDropDistance = 4f;
        [SerializeField] private float m_FlyVerticalRotation = 45f;
        [SerializeField] private Vector3 m_Aceleration = new Vector3(1f, 0f, 1f);
        [SerializeField, Range(.1f, 1f)]
        private float m_RotationSpeed = .1f;
        [SerializeField, Range(.01f, .5f)] private float m_Gravity = .04f;

        private NamedAnimancerComponent m_Animancer;
        private UnityInput m_Input;
        private UltimateCharacterLocomotion m_Locomotion;
        private List<Renderer> m_Renderers;
        private bool m_Opened;

        void Awake() {
            m_Input = GetComponentInParent<UnityInput>();
            m_Locomotion = GetComponentInParent<UltimateCharacterLocomotion>();
            m_Animancer = GetComponent<NamedAnimancerComponent>();
            m_Renderers = m_Input.GetComponentsInChildren<Renderer>(true).Where(r => !r.transform.IsChildOf(transform)).ToList();
        }

        void OnEnable() {
            m_Locomotion.UseRootMotionPosition = false;

            foreach (var ia in m_Locomotion.ItemAbilities)
                ia.Enabled = false;

            EnableAbility(m_Locomotion.Abilities, "Jump", false);
            m_Renderers.ForEach(r => r.enabled = false);

            transform.localPosition = new Vector3(0f, 1.2f, -.6f);
            transform.localRotation = Quaternion.Euler(m_FlyVerticalRotation, 0f, 0f);
        }

        void OnDisable() {
            m_Locomotion.UseRootMotionPosition = true;
            m_Locomotion.MotorAcceleration = new Vector3(.18f, 0f, .18f);
            m_Locomotion.MotorAirborneAcceleration = new Vector3(.18f, 0f, .18f);
            m_Locomotion.MotorRotationSpeed = 10f;
            m_Locomotion.GravityDirection = new Vector3(0f, -1f, 0f);

            foreach (var ia in m_Locomotion.ItemAbilities)
                ia.Enabled = true;

            EnableAbility(m_Locomotion.Abilities, "Jump", true);
            m_Renderers.ForEach(r => r.enabled = true);

            if (!m_Opened)
                m_Locomotion.GetComponent<CharacterHealthEx>().Die(m_Locomotion.transform.position, Vector3.zero, null);

            FindObjectOfType<CameraController>().CanChangePerspectives = true;
        }

        void Update() {
            if (!m_Locomotion || m_Locomotion.UseRootMotionPosition)
                return;

            m_Locomotion.UseRootMotionPosition = false;
            m_Locomotion.MotorAcceleration = m_Aceleration;
            m_Locomotion.MotorAirborneAcceleration = m_Aceleration;
            m_Locomotion.MotorRotationSpeed = m_RotationSpeed;

            if (!Physics.Raycast(m_Locomotion.transform.position, -m_Locomotion.transform.up, out RaycastHit hit, 1000f, m_RaycastMask))
                return;

            if (hit.distance <= m_MinOpenDistance && !m_Opened && m_Input.GetButtonDown("Action")) {
                GetComponentsInChildren<ParachuteCloth>(true).FirstOrDefault().Open();

                var gravityDirection = m_Locomotion.GetType().GetField("m_GravityDirection", BindingFlags.Instance | BindingFlags.NonPublic);
                gravityDirection.SetValue(m_Locomotion, new Vector3(0f, -m_Gravity, 0f));
                m_Animancer.Play(m_ParachuteIdle);

                m_Opened = true;
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;

                GetComponentsInChildren<TrailRenderer>().FirstOrDefault().enabled = false;
            }

            if (hit.distance <= m_MinDropDistance) {
                var pc = GetComponentsInChildren<ParachuteCloth>(true).FirstOrDefault();

                pc.transform.parent = null;
                pc.Drop(m_ClothRemoveDelay);

                //Destroy(gameObject);
                Addressables.ReleaseInstance(gameObject);
            }
        }

        void EnableAbility(Ability[] group, string abilityName, bool flag) {
            var ability = group.FirstOrDefault(a => a.GetType().Name == abilityName);
            if (ability != null)
                ability.Enabled = flag;
        }
    }
}
