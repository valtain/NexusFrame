using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NexusFrame
{
    [DefaultExecutionOrder(-1000)]
    public class ColdStartup : MonoBehaviour
    {
        [SerializeField] private PlaySessionType _sessionType = PlaySessionType.Exploration;
        private void Awake()
        {
#if UNITY_EDITOR
            var currentSceneName = SceneManager.GetActiveScene().name;
            if (SceneDirector.AreAllPrerequisitesLoadedFor(currentSceneName))
            {
                return;
            }

            var currentScene = SceneManager.GetActiveScene();
            foreach (var go in currentScene.GetRootGameObjects())
            {
                if (go != gameObject) go.SetActive(false);
            }
            SceneDirector.LoadColdStartupScene(_sessionType, currentSceneName).Forget();
#endif
        }

        private void Start()
        {
            gameObject.SetActive(false);
        }

    }
}