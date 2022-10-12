using UnityEngine.Playables;
using UnityEngine.SceneManagement;

public class SplashMenu : UIMenu
{
    public override UIMenu Show()
    {
        var director = GetComponentInChildren<PlayableDirector>();
        if (director != null)
        {
            director.stopped -= OnDirectorStopped;
            director.stopped += OnDirectorStopped;
        }

        return base.Show();
    }

    private void OnDirectorStopped(PlayableDirector d)
    {
        //SceneManager.LoadSceneAsync(1);
        
        // After the splash screen, we fade into the login menu
        UIManager.Instance.ShowOnly<LoginMenu>().FadeIn();
    }
}