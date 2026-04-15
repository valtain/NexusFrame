using Cysharp.Threading.Tasks;
using UnityEngine;

namespace NexusFrame
{
    public class PrefabInstanceStage: GameStageBase
    {
        public override string StageName => $"Prefab:{_uri}";

        public override bool IsLoaded => _isLoaded;

        public override void RegisterAnchor(GameStageAnchorBase anchor) => Anchor = anchor;

        public override async UniTask Load()
        {
            var prefab = await Resources.LoadAsync<GameObject>(_uri);
            await UniTask.Yield();
            var stageRoot = GamePlaySystem.Instance.PrefabInstanceZoneRoot;
            _instance = GameObject.Instantiate(prefab, stageRoot, false) as GameObject;
            _isLoaded = _instance != null;
        }

        public override UniTask Unload()
        {
            if (_isLoaded)
            {
                GameObject.Destroy(_instance);
                _instance = null;
            }
            return UniTask.CompletedTask;
        }

        public override void Clear()
        {
            StageType = GameStageType.PrefabInstance;
            DoOverrideStage = false;
            _uri = string.Empty;
            _instance = null;
            Anchor = null;
        }

        public PrefabInstanceStage() : this(string.Empty, false) {}

        public PrefabInstanceStage(string uri, bool doOverridePrevStage) : base(GameStageType.PrefabInstance, doOverridePrevStage)
        {
            _uri = uri;
            _instance = null;
            Anchor = null;
        }

        private string _uri = string.Empty;
        private GameObject _instance = default;
        private bool _isLoaded = false;
    }
}
