using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Playables;

namespace IEdgeGames {

    [RequireComponent(typeof(PlayableDirector))]
    public class SplashScreen : MonoBehaviour {

        void Awake() {
            var director = GetComponent<PlayableDirector>();

            if (!director)
                return;

            director.stopped += (d) => SceneManager.LoadSceneAsync(1);
        }
    }
}
