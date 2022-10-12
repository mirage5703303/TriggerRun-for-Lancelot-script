using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace IEdgeGames {

    public class ParachuteCloth : MonoBehaviour {

        Animator animator;
        Cloth cloth;
        SkinnedMeshRenderer skRend1, skRend2;
        bool opened;
        bool dropped;

        private void Awake() {
            animator = transform.GetComponent<Animator>();
            cloth = transform.Find("mesh").Find("mesh_Stage2").GetComponent<Cloth>();
            skRend1 = transform.Find("mesh").Find("mesh_Stage1").GetComponent<SkinnedMeshRenderer>();
            skRend2 = transform.Find("mesh").Find("mesh_Stage2").GetComponent<SkinnedMeshRenderer>();
            Init();
        }

        void Init() {
            opened = false;
            dropped = false;
            skRend1.enabled = false;
            skRend2.enabled = false;
        }

        IEnumerator IE_Opening() {
            skRend1.enabled = true;
            animator.Play("Opening");
            yield return new WaitForSeconds(1f);
            skRend1.enabled = false;
            skRend2.enabled = true;
            yield return new WaitForSeconds(1f);
        }

        /// <summary>
        /// Opening animation
        /// </summary>
        public void Open() {
            if (opened)
                return;

            opened = true;
            StartCoroutine(IE_Opening());
        }

        /// <summary>
        /// Dropping parachute to the ground
        /// </summary>
        /// <param name="destroyDelay"></param>
        public void Drop(float destroyDelay) {
            if (dropped)
                return;

            dropped = true;
            cloth.stretchingStiffness = 1f;
            cloth.bendingStiffness = 0.7f;
            cloth.worldVelocityScale = 0f;
            cloth.worldAccelerationScale = 0f;
            cloth.damping = 0f;
            cloth.externalAcceleration = new Vector3(0f, -10f, 0f);
            cloth.randomAcceleration = Vector3.zero;
            transform.parent = null;

            var rBody = GetComponent<Rigidbody>();
            rBody.isKinematic = false;
            //rBody.AddForce(-transform.forward * 10f);
            //rBody.AddRelativeTorque(-1000000f, 0f, 0f);

            Destroy(gameObject, destroyDelay);
        }
    }
}
