using System;
using System.Collections;
using BeauRoutine;
using TMPro;
using UnityEngine.SceneManagement;

public class LoadingMenu : UIMenu
{
    public string DefaultWaitText = "LOADING...";
    public TextMeshProUGUI WaitText;
    public LoadingMenu WaitFor(string waitText, Action onLoadAction, Func<bool> waitCondition, float waitConditionInterval = 0.5f)
    {
        WaitText.text = string.IsNullOrEmpty(waitText) ? DefaultWaitText : waitText;
        
        IEnumerator CustomWaiter()
        {
            yield return Routine.WaitCondition(() => waitCondition?.Invoke() ?? true, waitConditionInterval);
            Hide();
            onLoadAction?.Invoke();
        }
        
        Routine.Start(CustomWaiter());

        return this;
    }
    
    public LoadingMenu WaitForSceneLoad(string waitText, int buildIndex, Action onLoadAction)
    {
        WaitText.text = string.IsNullOrEmpty(waitText) ? DefaultWaitText : waitText;
        
        IEnumerator Waiter()
        {
            yield return Routine.WaitCondition(() => SceneManager.GetActiveScene().buildIndex == buildIndex, 0.5f);
            Hide();
            onLoadAction?.Invoke();
        }
        Routine.Start(Waiter());
        return this;
    }
}