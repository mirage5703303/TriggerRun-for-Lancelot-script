using UnityEngine;
using UnityEngine.UI;

public class MenuBackBehaviorButton : MonoBehaviour
{
    private void Awake()
    {
        var button = GetComponent<Button>();
        if (button != null)
        {
            var parent = GetComponentInParent<UIMenu>();
            if (parent != null)
            {
                button.onClick.AddListener(() =>
                {
                    parent.Hide();
                });
            }
        }
    }
}