using Cysharp.Threading.Tasks;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace NexusFrame
{
    public class SceneDirector: MonoPreload<SceneDirector>
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize() => ResetInstance();

        private string _currentMasterSceneName = string.Empty;
        private List<SceneType> _loadedPrerequisiteScenes = null;

        protected override void Awake()
        {
            base.Awake();
            _currentMasterSceneName = SceneManager.GetActiveScene().name;
            _loadedPrerequisiteScenes = new () { SceneType.Preload };
        }

        private async UniTask LoadSceneInternal(string sceneName, bool isMasterScene = false)
        {
            Debug.Assert(!string.IsNullOrEmpty(sceneName));

            await CheckPrerequisiteScenes(sceneName);

            if (!string.IsNullOrEmpty(_currentMasterSceneName))
            {
                await SceneManager.UnloadSceneAsync(_currentMasterSceneName);
            }

            _currentMasterSceneName = sceneName;
            await SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        }

        private async UniTask CheckPrerequisiteScenes(string sceneName)
        {
            var sceneType = SceneUtils.GetSceneType(sceneName);
            Debug.Assert(sceneType != SceneType.NaV && !SceneUtils.IsPrerequisiteScene(sceneType));

            var prerequisiteSceneTypes = SceneUtils.GetPrerequisiteScenes(sceneType);
            // ReSharper disable once HeapView.ObjectAllocation.Possible
            foreach (var prerequisiteSceneType in prerequisiteSceneTypes)
            {
                if (_loadedPrerequisiteScenes.Contains(prerequisiteSceneType))
                {
                    continue;
                }
                _loadedPrerequisiteScenes.Add(prerequisiteSceneType);
                var prerequisiteSceneName = SceneUtils.GetSpecialSceneName(prerequisiteSceneType);
                await SceneManager.LoadSceneAsync(prerequisiteSceneName, LoadSceneMode.Additive);
            }
        }

        public static bool IsPreloadSceneLoaded() =>
#if UNITY_EDITOR
            SceneDirector.HasInstance;
#else
            true; // 실제 게임에서는 Preload 씬이 항상 로드되어 있다고 가정
#endif

        public static bool IsPrerequisiteSceneLoaded(SceneType sceneType)
        {
            return
                sceneType != SceneType.NaV &&
                IsPreloadSceneLoaded() &&
                Instance._loadedPrerequisiteScenes.Contains(sceneType);
        }

        public static async UniTask<bool> LoadScene(string sceneName)
        {
            if (!HasInstance)
            {
                await SceneManager.LoadSceneAsync(SceneUtils.PreloadSceneName, LoadSceneMode.Additive);
            }
            await Instance.LoadSceneInternal(sceneName);
            return true;
        }
    }
}