using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using NexusFrame.Extensions;
using UnityEngine;

namespace NexusFrame
{
    public enum GameStageType
    {
        None = -1,
        Level,
        AdditiveLevel,
        SubLevel,
        PrefabInstance,
        GoPrevLevel
    }


    [System.Serializable]
    public struct GameStageDesc
    {
        public GameStageType StageType;
        public string AssetPath;
        public bool DoOverrideStage;
        public GameStageDesc(GameStageType type, string path, bool doOverrideStage)
        {
            StageType = type;
            AssetPath = path;
            DoOverrideStage = doOverrideStage;
        }
    }


    public abstract class GameStageBase
    {
        public GameStageBase(GameStageType stageType, bool doOverrideStage) { StageType = stageType; DoOverrideStage = doOverrideStage; }
        public GameStageType StageType {get; protected set;} = GameStageType.None;
        public bool DoOverrideStage {get; protected set;} = false;
        public abstract bool IsLoaded {get;}
        public abstract string StageName {get;}
        public virtual IReadOnlyList<GameStageBase> SubStages => _emptySubStages;
        public GameStageAnchorBase Anchor {get; protected set;} = null;
        public abstract void RegisterAnchor(GameStageAnchorBase anchor);

        public abstract UniTask Load();
        public abstract UniTask Unload();

        public virtual void OnSessionSlept()
        {
            foreach(var subStage in SubStages)
            {
                subStage.OnSessionSlept();
            }
            Anchor.gameObject.SetActiveSafe(false);
        }

        public virtual void OnSessionResumed()
        {
            foreach(var subStage in SubStages)
            {
                subStage.OnSessionResumed();
            }
            Anchor.gameObject.SetActiveSafe(true);
        }

        public abstract void Clear();

        protected static IReadOnlyList<GameStageBase> _emptySubStages = new List<GameStageBase>();
    }

    public abstract class GameStageAnchorBase: MonoBehaviour
    {
        [field: SerializeField]
        public string Id { get; protected set;}

        public bool DoOverrideStage => false; // [TODO]

        public IReadOnlyList<GameStageAnchorBase> SubStageAnchorList => _subStageAnchorList;

        public virtual void Awake()
        {
            // sub stage 는 비활성화 상태로 시작한다.
            foreach (GameStageAnchorBase subStage in _subStageAnchorList)
            {
                subStage.gameObject.SetActiveSafe(false);
            }
            GamePlaySystem.Instance.RegisterAnchorOnce(this);
        }

        [SerializeField] protected List<GameStageAnchorBase> _subStageAnchorList = default;
    }
}
