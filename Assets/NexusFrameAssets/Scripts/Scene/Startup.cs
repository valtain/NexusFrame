using Cysharp.Threading.Tasks;
using UnityEngine;

namespace NexusFrame
{
    public class Startup : MonoBehaviour
    {
        private void Start()
        {
            StartAsync().Forget();
        }

        private async UniTask StartAsync()
        {
            await SceneDirector.EnsurePreloadReady();
        }
    }
}