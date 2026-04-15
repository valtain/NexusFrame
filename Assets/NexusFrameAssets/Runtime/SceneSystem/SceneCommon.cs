using System.Collections.Generic;

namespace NexusFrame
{
    public enum SceneType
    {
        None = -1,
        Preload = 0, // 자동으로 로딩되는 씬
        Splash, // 이후 Preload 씬이 필요
        Title,
        MainMenu,
        GamePlay = 100 - 1,
        Level = 100, // Preload, GamePlay 씬이 필요
        Test = 1000, // 테스트용 씬
    }

    public static class SceneUtils
    {
        public static readonly string PreloadSceneName = "Preload";
        public static readonly string GamePlaySceneName = "GamePlay";
        private static readonly IReadOnlyDictionary<string, SceneType> _sceneTypeDic = new Dictionary<string, SceneType>
        {
            { PreloadSceneName, SceneType.Preload },
            { "Splash", SceneType.Splash },
            { "Title", SceneType.Title },
            { "MainMenu", SceneType.MainMenu },
            { "World0", SceneType.Level },
            { "World1", SceneType.Level },
            { "Battle0", SceneType.Level },
            { GamePlaySceneName, SceneType.GamePlay },
            { "Intro", SceneType.Title },
        };

        public static SceneType GetSceneType(string sceneName)
        {
            return string.IsNullOrEmpty(sceneName)
                ? SceneType.None
                : _sceneTypeDic.GetValueOrDefault(sceneName, SceneType.Test);
        }

        public static IReadOnlyCollection<SceneType> GetPrerequisiteScenes(SceneType sceneType)
        {
            return
                (int)sceneType > (int)SceneType.GamePlay ? _gameplayRequisiteScenes :
                (int)sceneType > (int)SceneType.Preload ? _preloadRequisiteScenes :
                _noRequisiteScenes;
        }

        public static bool IsGamePlayRequired(SceneType sceneType)
            => (int)sceneType > (int)SceneType.GamePlay;

        public static string GetSpecialSceneName(SceneType sceneType)
        {
            return sceneType switch
            {
                SceneType.Preload => PreloadSceneName,
                SceneType.GamePlay => GamePlaySceneName,
                _ => string.Empty
            };
        }

        public static bool IsPrerequisiteScene(SceneType sceneType)
        {
#pragma warning disable IDE0072 // Add missing cases
            return sceneType switch
            {
                SceneType.Preload => true,
                SceneType.GamePlay => true,
                _ => false
            };
#pragma warning restore IDE0072 // Add missing cases
        }

        private static readonly IReadOnlyCollection<SceneType> _preloadRequisiteScenes = new List<SceneType>() { SceneType.Preload };
        private static readonly IReadOnlyCollection<SceneType> _gameplayRequisiteScenes = new List<SceneType>() { SceneType.Preload, SceneType.GamePlay };
        private static readonly IReadOnlyCollection<SceneType> _noRequisiteScenes = new List<SceneType>();
    }
}