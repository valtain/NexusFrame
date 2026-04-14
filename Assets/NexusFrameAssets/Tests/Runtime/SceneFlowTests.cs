using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Cysharp.Threading.Tasks;

namespace NexusFrame.Tests.Runtime
{
    /// <summary>
    /// 씬 전환 흐름과 전제 조건 씬 자동 로딩을 검증하는 통합 테스트.
    /// Window > General > Test Runner > PlayMode 에서 실행.
    /// </summary>
    public class SceneFlowTests
    {
        private const float TimeoutSeconds = 15f;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            // 씬 언로드 전 진행 중인 페이드 효과 완료 대기
            // TransitionUi.End()의 페이드 아웃이 끝나기 전에 언로드하면 Graphic 접근 에러 발생
            yield return new WaitUntil(() => !UiSystem.IsTransitioning);

            // 언로드 순서: 콘텐츠 씬 → 전제 조건 씬(GamePlay → Preload)
            // Preload를 먼저 언로드하면 싱글톤이 사라져 이후 씬 언로드 중 에러 발생
            var contentScenes = new List<string>();
            var prerequisiteScenes = new List<string>();

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.name.StartsWith("InitTest") || scene.name.StartsWith("playmode"))
                    continue;

                if (SceneUtils.IsPrerequisiteScene(SceneUtils.GetSceneType(scene.name)))
                    prerequisiteScenes.Add(scene.name);
                else
                    contentScenes.Add(scene.name);
            }

            foreach (var name in contentScenes)
            {
                var scene = SceneManager.GetSceneByName(name);
                if (scene.IsValid() && scene.isLoaded)
                    yield return SceneManager.UnloadSceneAsync(name);
            }

            // GamePlay를 Preload보다 먼저 언로드
            prerequisiteScenes.Sort((a, b) =>
                (int)SceneUtils.GetSceneType(b) - (int)SceneUtils.GetSceneType(a));

            foreach (var name in prerequisiteScenes)
            {
                var scene = SceneManager.GetSceneByName(name);
                if (!scene.IsValid() || !scene.isLoaded) continue;

                // Unity는 마지막 씬 언로드를 허용하지 않으므로 임시 씬 확보
                // 이미 존재하면 재사용하여 중복 생성 방지
                if (SceneManager.sceneCount == 1)
                {
                    if (!SceneManager.GetSceneByName("_TeardownTemp").IsValid())
                        SceneManager.CreateScene("_TeardownTemp");
                    yield return null;
                }

                yield return SceneManager.UnloadSceneAsync(name);
            }
        }

        /// <summary>
        /// Scenario 1: Splash에서 시작 → MainMenu New Game → World0
        /// - Preload는 Splash 최초 로드 시 자동 로딩되어야 한다
        /// - GamePlay는 World0 로드 시작 전에 먼저 로딩되어야 한다
        /// </summary>
        [UnityTest]
        public IEnumerator FullFlow_SplashToWorld0_LoadsPrerequisitesInOrder()
        {
            // 1단계: 실제 게임 시작처럼 Splash 씬으로 직접 이동
            // Startup.cs가 EnsurePreloadReady()를 호출해 Preload 자동 로드
            yield return SceneManager.LoadSceneAsync("Splash", LoadSceneMode.Single);

            yield return WaitUntilLoaded("Preload");
            Assert.IsTrue(SceneDirector.HasInstance,
                "Preload 로드 직후 SceneDirector 인스턴스가 생성되어야 한다");

            // Splash는 일정 시간 후 Title로 자동 전환되므로 둘 중 하나 대기
            yield return WaitUntilAnyLoaded("Splash");
            Assert.IsTrue(SceneManager.GetSceneByName("Splash").isLoaded,
                "Splash 씬이 로드되어야 한다");
            yield return WaitUntilAnyLoaded("Title");
            Assert.IsTrue(SceneManager.GetSceneByName("Title").isLoaded,
                "Title 씬이 로드되어야 한다");
            // 2단계: "아무 키" 역할 — TitleController가 하는 것과 동일한 API 호출
            SceneDirector.LoadScene("MainMenu").Forget();

            yield return WaitUntilLoaded("MainMenu");
            Assert.IsTrue(SceneManager.GetSceneByName("MainMenu").isLoaded,
                "MainMenu 씬이 로드되어야 한다");
            Assert.IsFalse(SceneManager.GetSceneByName("Splash").isLoaded,
                "Splash는 언로드되어야 한다");
            Assert.IsFalse(SceneManager.GetSceneByName("Title").isLoaded,
                "Title은 언로드되어야 한다");

            // 3단계: New Game → World0 로드 (GamePlay 전제 조건 자동 로딩)
            bool gamePlayLoadedBeforeWorld0 = false;
            SceneDirector.LoadScene("World0").Forget();

            yield return WaitUntilLoaded("GamePlay");
            // GamePlay 로드 직후 World0가 아직 없어야 '먼저 로드됨'이 성립
            gamePlayLoadedBeforeWorld0 = !SceneManager.GetSceneByName("World0").isLoaded;

            yield return WaitUntilLoaded("World0");

            Assert.IsTrue(gamePlayLoadedBeforeWorld0,
                "GamePlay는 World0보다 먼저 로드되어야 한다");
            Assert.IsTrue(SceneManager.GetSceneByName("Preload").isLoaded,
                "Preload는 전체 흐름 동안 유지되어야 한다");
            Assert.IsTrue(SceneManager.GetSceneByName("GamePlay").isLoaded,
                "GamePlay가 로드되어야 한다");
            Assert.IsTrue(SceneManager.GetSceneByName("World0").isLoaded,
                "World0가 로드되어야 한다");
            Assert.IsFalse(SceneManager.GetSceneByName("MainMenu").isLoaded,
                "이전 콘텐츠 씬(MainMenu)은 언로드되어야 한다");
        }

        /// <summary>
        /// Scenario 2: World0에서 직접 시작 (Editor Cold Start)
        /// - ColdStartup이 Preload와 GamePlay를 자동 로드하고 World0를 재로드해야 한다
        /// </summary>
        [UnityTest]
        public IEnumerator ColdStart_FromWorld0_AutoLoadsPrerequisitesAndReloadsWorld0()
        {
            // World0를 단독으로 로드 (에디터에서 씬 직접 Play 시뮬레이션)
            yield return SceneManager.LoadSceneAsync("World0", LoadSceneMode.Single);
            yield return null; // ColdStartup.Awake() 실행 대기

            // ColdStartup → SceneDirector.LoadScene("World0")
            //   → EnsurePreloadReady() : Preload 로드 + SceneDirector 생성
            //   → EnsurePrerequisitesLoaded() : GamePlay 로드
            //   → World0 재로드

            yield return WaitUntilLoaded("Preload");
            Assert.IsTrue(SceneDirector.HasInstance,
                "Preload 로드 후 SceneDirector가 생성되어야 한다");
            Assert.IsTrue(SceneDirector.IsPrerequisiteSceneLoaded(SceneType.Preload),
                "SceneDirector가 Preload를 전제 조건으로 추적해야 한다");

            yield return WaitUntilLoaded("GamePlay");
            Assert.IsTrue(SceneManager.GetSceneByName("GamePlay").isLoaded,
                "GamePlay가 자동 로드되어야 한다");
            Assert.IsTrue(SceneDirector.IsPrerequisiteSceneLoaded(SceneType.GamePlay),
                "SceneDirector가 GamePlay를 전제 조건으로 추적해야 한다");

            yield return WaitUntilLoaded("World0");
            Assert.IsTrue(SceneManager.GetSceneByName("World0").isLoaded,
                "World0가 재로드되어야 한다");
        }

        // ─── Helper ──────────────────────────────────────────────────────────

        /// <summary>씬이 로드될 때까지 프레임 단위로 대기. 타임아웃 초과 시 테스트 실패.</summary>
        private IEnumerator WaitUntilLoaded(string sceneName)
        {
            float elapsed = 0f;
            while (!SceneManager.GetSceneByName(sceneName).isLoaded)
            {
                elapsed += Time.deltaTime;
                if (elapsed >= TimeoutSeconds)
                {
                    Assert.Fail($"씬 '{sceneName}'이 {TimeoutSeconds}초 내에 로드되지 않았다");
                    yield break;
                }
                yield return null;
            }
        }

        /// <summary>지정한 씬 중 하나라도 로드될 때까지 대기. 타임아웃 초과 시 테스트 실패.</summary>
        private IEnumerator WaitUntilAnyLoaded(params string[] sceneNames)
        {
            float elapsed = 0f;
            while (true)
            {
                foreach (var name in sceneNames)
                {
                    if (SceneManager.GetSceneByName(name).isLoaded)
                        yield break;
                }
                elapsed += Time.deltaTime;
                if (elapsed >= TimeoutSeconds)
                {
                    Assert.Fail($"씬 '{string.Join("', '", sceneNames)}' 중 어느 것도 {TimeoutSeconds}초 내에 로드되지 않았다");
                    yield break;
                }
                yield return null;
            }
        }
    }
}
