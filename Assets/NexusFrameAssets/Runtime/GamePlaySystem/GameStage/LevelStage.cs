using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NexusFrame
{
    public class LevelStage : GameStageBase
    {
        public override string StageName => _sceneName;
        public override bool IsLoaded =>_scene.IsValid() && _scene.isLoaded;

        public override IReadOnlyList<GameStageBase> SubStages => _subStages;

        public LevelStage() : this(GameStageType.Level, string.Empty, false)
        {
        }

        public LevelStage(GameStageType stageType, string sceneName, bool doOverrideStage) : base(stageType, doOverrideStage)
        {
            _sceneName = sceneName;
        }

        public void Reset(string sceneName, bool doOverrideStage)
        {
            DoOverrideStage = doOverrideStage;
            _sceneName = sceneName;
            Anchor = null;
            _subStages.Clear();
        }

        public override void RegisterAnchor(GameStageAnchorBase anchor)
        {
            Anchor = anchor;
            _subStages.Clear();
            if (anchor.SubStageAnchorList is { Count: > 0 } == false)
            {
                return;
            }
            var anchorCount = anchor.SubStageAnchorList.Count;
            if (_subStages.Capacity < anchorCount)
            {
                _subStages.Capacity = anchorCount;
            }
            foreach (var subStageAnchor in anchor.SubStageAnchorList)
            {
                _subStages.Add(new SubLevelStage(subStageAnchor));
            }
        }

        public override async UniTask Load()
        {
            if (_scene.isLoaded)
            {
                return;
            }

            _scene = await SceneDirector.LoadSceneAdditive(_sceneName);
            await UniTask.Yield();
        }


        public override async UniTask Unload()
        {
            Debug.Assert(_scene.isLoaded, $"[Stage] {_sceneName} is not loaded.");
            await SceneDirector.UnloadScene(_sceneName);
            await UniTask.Yield();
            ClearSubStages();
        }

        private void ClearSubStages()
        {
            foreach (var subStage in _subStages)
            {
                subStage.Clear();
            }
            _subStages.Clear();
        }

        public override void Clear()
        {
            ClearSubStages();
            DoOverrideStage = false;
            _sceneName = string.Empty;
            Anchor = null;
        }

        private string _sceneName;
        private Scene _scene;
        private List<GameStageBase> _subStages = new();
    }
}
