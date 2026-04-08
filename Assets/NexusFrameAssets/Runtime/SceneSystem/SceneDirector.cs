using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NexusFrame
{
    /// <summary>
    /// 씬 로딩/언로딩의 진입점을 제공하는 싱글톤 씬 관리자.
    /// <para>
    /// - <see cref="LoadScene"/>을 통해 콘텐츠 씬을 전환하며, 전환 전후로 <see cref="TransitionUi"/> 효과를 자동 적용한다.
    /// - 콘텐츠 씬(<c>_loadedContentScenes</c>)과 선행 씬(<c>_loadedPrerequisiteScenes</c>)을 별도로 추적한다.
    /// - 새 씬 로딩 시 <see cref="SceneUtils.GetPrerequisiteScenes"/>로 필요한 선행 씬을 자동으로 추가 로드한다.
    /// - GamePlay 씬 내부의 씬 전환은 별도 관리자에서 담당할 예정이므로, 이 클래스에서는 관리하지 않는다.
    /// - <see cref="EnsurePreloadReady"/>를 통해 Preload 씬이 없는 상태에서도 안전하게 진입 가능하다.
    /// </para>
    /// </summary>
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
                if (!SceneUtils.IsPrerequisiteScene(sceneType) && sceneType != SceneType.None)
                {
                    _loadedContentScenes.Add(scene.name);
                }
            }
        }

        // ── Public Entry Points ───────────────────────────────────────────

        /// <summary>기존 콘텐츠 씬을 언로드하고 새 씬으로 전환한다.</summary>
        public static async UniTask<Scene> LoadScene(string sceneName)
        {
            Debug.Assert(!string.IsNullOrEmpty(sceneName));
            await EnsurePreloadReady();
            return await Instance.LoadSceneCore(sceneName, unloadContents: true);
        }

        /// <summary>기존 콘텐츠 씬을 유지한 채 새 씬을 추가로 로드한다.</summary>
        public static async UniTask<Scene> LoadSceneAdditive(string sceneName)
        {
            Debug.Assert(!string.IsNullOrEmpty(sceneName));
            await EnsurePreloadReady();
            return await Instance.LoadSceneCore(sceneName, unloadContents: false);
        }

        public static async UniTask LoadColdStartupScene(string sceneName)
        {
            Debug.Assert(!string.IsNullOrEmpty(sceneName));
            var sceneType = SceneUtils.GetSceneType(sceneName);

            if (SceneUtils.IsGamePlayRequired(sceneType) == false)
            {
                await LoadScene(sceneName);
                return;
            }

            await EnsurePreloadReady();
            await Instance.LoadGamePlaySceneByColdStartUp(sceneName);
        }

        public static async UniTask UnloadScene(string sceneName)
        {
            Debug.Assert(!string.IsNullOrEmpty(sceneName));
            Debug.Assert(HasInstance == true);
            await Instance.UnloadSceneCore(sceneName);
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

        private async UniTask<Scene> LoadSceneCore(string sceneName, bool unloadContents)
        {
            var sceneType = SceneUtils.GetSceneType(sceneName);
            Debug.Assert(sceneType != SceneType.None, $"SceneType not found: {sceneName}");
            Debug.Assert(!SceneUtils.IsPrerequisiteScene(sceneType), $"Prerequisite scene cannot be loaded directly: {sceneName}");

            await TransitionUi.Instance.Begin(TransitionEffectType.Fade);
            await EnsurePrerequisitesLoaded(sceneType);
            if (unloadContents)
            {
                await UnloadContentScenes();
            }
            _loadedContentScenes.Add(sceneName);
            await SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            var loadedScene = SceneManager.GetSceneByName(sceneName);
            await TransitionUi.Instance.End();
            return loadedScene;
        }

        private async UniTask LoadGamePlaySceneByColdStartUp(string sceneName)
        {
            var sceneType = SceneUtils.GetSceneType(sceneName);
            await TransitionUi.Instance.Begin(TransitionEffectType.Fade);
            await UnloadContentScenes();
            await EnsurePrerequisitesLoaded(sceneType);
            await GamePlaySystem.Instance.LaunchSessionAtColdStartup(sceneName);
            await TransitionUi.Instance.End();
        }

        private async UniTask UnloadSceneCore(string sceneName)
        {
            var sceneType = SceneUtils.GetSceneType(sceneName);
            Debug.Assert(sceneType != SceneType.None, $"SceneType not found: {sceneName}");
            Debug.Assert(_loadedContentScenes.Contains(sceneName), $"Scene is not loaded: {sceneName}");
            Debug.Assert(!SceneUtils.IsPrerequisiteScene(sceneType), $"Prerequisite scene cannot be unloaded directly: {sceneName}");
            await TransitionUi.Instance.Begin(TransitionEffectType.Fade);
            _loadedContentScenes.Remove(sceneName);
            await SceneManager.UnloadSceneAsync(sceneName);
            await TransitionUi.Instance.End();
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
            foreach (var scene in prerequisiteScenes)
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