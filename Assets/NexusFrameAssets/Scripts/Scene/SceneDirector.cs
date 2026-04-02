using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NexusFrame
{
    public class SceneDirector : MonoPreload<SceneDirector>
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize() => ResetInstance();

        [field: SerializeField]
        public Camera MainCamera {get; private set;} = default;
        public Transform MainCameraTransform {get; private set;}

        private readonly HashSet<string> _loadedContentScenes = new();
        private readonly HashSet<SceneType> _loadedPrerequisiteScenes = new();

        protected override void Awake()
        {
            base.Awake();
            MainCamera = Camera.main;
            MainCameraTransform = MainCamera.transform;
            _loadedPrerequisiteScenes.Add(SceneType.Preload);

            // Preload 씬 로딩 시점에 이미 떠있는 씬들을 content로 등록
            // Splash(Startup), 또는 ColdStartup 시나리오 대응
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                var sceneType = SceneUtils.GetSceneType(scene.name);
                if (!SceneUtils.IsPrerequisiteScene(sceneType) && sceneType != SceneType.NaV)
                {
                    _loadedContentScenes.Add(scene.name);
                }
            }
        }

        // ── Public Entry Point ────────────────────────────────────────────

        public static async UniTask LoadScene(string sceneName)
        {
            Debug.Assert(!string.IsNullOrEmpty(sceneName));
            await EnsurePreloadReady();
            await TransitionUi.Instance.Begin(TransitionEffectType.Fade);
            await Instance.LoadSceneInternal(sceneName);
            await TransitionUi.Instance.End();
        }

        public static async UniTask EnsurePreloadReady()
        {
            if (HasInstance)
            {
                return;
            }
            await SceneManager.LoadSceneAsync(SceneUtils.PreloadSceneName, LoadSceneMode.Additive);
            await UniTask.WaitUntil(() => HasInstance);
        }

        // ── Internal ──────────────────────────────────────────────────────

        private async UniTask LoadSceneInternal(string sceneName)
        {
            var sceneType = SceneUtils.GetSceneType(sceneName);
            Debug.Assert(sceneType != SceneType.NaV, $"SceneType not found: {sceneName}");
            Debug.Assert(!SceneUtils.IsPrerequisiteScene(sceneType), $"Prerequisite scene cannot be loaded directly: {sceneName}");

            bool isGamePlayAlreadyLoaded = _loadedPrerequisiteScenes.Contains(SceneType.GamePlay);

            await EnsurePrerequisitesLoaded(sceneType);
            if (!isGamePlayAlreadyLoaded)
            {
                await UnloadContentScenes();
            }

            _loadedContentScenes.Add(sceneName);
            await SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        }

        private async UniTask EnsurePrerequisitesLoaded(SceneType sceneType)
        {
            var required = SceneUtils.GetPrerequisiteScenes(sceneType);
            foreach (var prerequisiteType in required)
            {
                if (_loadedPrerequisiteScenes.Contains(prerequisiteType))
                {
                    continue;
                }

                _loadedPrerequisiteScenes.Add(prerequisiteType);
                var prerequisiteSceneName = SceneUtils.GetSpecialSceneName(prerequisiteType);
                await SceneManager.LoadSceneAsync(prerequisiteSceneName, LoadSceneMode.Additive);
            }
        }

        private async UniTask UnloadContentScenes()
        {
            foreach (var contentScene in _loadedContentScenes)
            {
                await SceneManager.UnloadSceneAsync(contentScene);
            }
            _loadedContentScenes.Clear();
        }

        // ── Query ─────────────────────────────────────────────────────────

        public static bool IsPrerequisiteSceneLoaded(SceneType sceneType)
        {
            return HasInstance && Instance._loadedPrerequisiteScenes.Contains(sceneType);
        }

        public static bool AreAllPrerequisitesLoadedFor(string sceneName)
        {
            if (!HasInstance)
            {
                return false;
            }

            var sceneType = SceneUtils.GetSceneType(sceneName);
            var prerequisiteScenes = SceneUtils.GetPrerequisiteScenes(sceneType);
            foreach(var scene in prerequisiteScenes)
            {
                if (Instance._loadedPrerequisiteScenes.Contains(scene) == false)
                {
                    return false;
                }
            }
            return true;
        }
    }
}