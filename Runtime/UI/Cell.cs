using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

namespace IEdgeGames {

    public class Cell : FancyCell<MapInfo, CellContext> {

        [SerializeField] Animator animator = default;
        //[SerializeField] Text message = default;
        [SerializeField] Image image;
        [SerializeField] Button button;

        static class AnimatorHash {
            public static readonly int Scroll = Animator.StringToHash("scroll");
        }

        public override void Initialize() {
            button.onClick.AddListener(() => Context.OnCellClicked?.Invoke(Index));
        }

        public override void UpdateContent(MapInfo itemData) {
            //message.text = itemData.Name;

            var selected = Context.SelectedIndex == Index;

            image.sprite = itemData.icon;
            image.color = selected
                ? new Color32(255, 255, 255, 255)
                : new Color32(255, 255, 255, 77);
        }

        public override void UpdatePosition(float position) {
            currentPosition = position;

            if (animator.isActiveAndEnabled) {
                animator.Play(AnimatorHash.Scroll, -1, position);
            }

            animator.speed = 0;
        }

        // GameObject が非アクティブになると Animator がリセットされてしまうため
        // 現在位置を保持しておいて OnEnable のタイミングで現在位置を再設定します
        float currentPosition = 0;

        void OnEnable() => UpdatePosition(currentPosition);
    }
}
