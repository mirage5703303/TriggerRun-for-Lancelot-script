using System.Linq;
using UnityEngine;

namespace IEdgeGames {

    public class FPSShadowFixer : MonoBehaviour {

        void Awake() {
            var overlayMask = LayerMask.NameToLayer("Overlay");
            var renderers = GetComponentsInChildren<Renderer>(true).Where(r => r.gameObject.layer == overlayMask);

            foreach (var renderer in renderers)
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }
}
