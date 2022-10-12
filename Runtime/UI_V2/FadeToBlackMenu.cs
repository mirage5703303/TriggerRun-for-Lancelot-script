using System;

public class FadeToBlackMenu : UIMenu
{
    public static void FadeToBlack(Action fadeCompleteAction, float fadeInDuration = 0.5f, float fadeOutDuration = 1.0f, float delayDuration = 0.5f)
    {
        UIManager.Instance.TryShow<FadeToBlackMenu>().FadeIn(fadeInDuration, 0.0f)
            .OnComplete(() =>
            {
                fadeCompleteAction?.Invoke();
                UIManager.Instance.GetMenu<FadeToBlackMenu>().FadeOut(fadeOutDuration, 1.0f).DelayBy(delayDuration).OnComplete(() =>
                {
                    UIManager.Instance.TryHide<FadeToBlackMenu>();
                });
            });
    }
}