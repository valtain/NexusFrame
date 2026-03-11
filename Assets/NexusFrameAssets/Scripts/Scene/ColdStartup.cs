using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NexusFrame
{
    [DefaultExecutionOrder(-1000)]
    public class ColdStartup : MonoBehaviour
    {
        private void Awake()
        {
#if UNITY_EDITOR
            var currentScene = SceneManager.GetActiveScene();
            foreach (var go in currentScene.GetRootGameObjects())
            {
                if (go != gameObject) go.SetActive(false);
            }

            StartAsync().Forget();
#endif
        }

        private async UniTask StartAsync()
        {
            var currentSceneName = SceneManager.GetActiveScene().name;
            await SceneDirector.LoadScene(currentSceneName);
        }
    }
}