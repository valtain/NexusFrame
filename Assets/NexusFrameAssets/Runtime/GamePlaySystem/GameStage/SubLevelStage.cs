using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using NexusFrame.Extensions;

namespace NexusFrame
{
    public class SubLevelStage: GameStageBase
    {
        public override string StageName => $"SubLevel:{Id}";

        public override void RegisterAnchor(GameStageAnchorBase anchor)
        {
            Debug.Assert(
                Anchor != null && anchor == null,
                "Anchor is not matched");
        }

        public override UniTask Load()
        {
            Anchor.gameObject.SetActiveSafe(true);
            return UniTask.CompletedTask;
        }

        public override UniTask Unload()
        {
            Anchor.gameObject.SetActiveSafe(false);
            return UniTask.CompletedTask;
        }

        public override void Clear()
        {
            Anchor = null;
            DoOverrideStage = false;
            Id = string.Empty;
        }

        public override bool IsLoaded => Anchor.gameObject.activeSelf;

        public string Id { get; private set; } = string.Empty;

        public SubLevelStage(): this(null) {}

        public SubLevelStage(GameStageAnchorBase anchor) : base(GameStageType.SubLevel, false)
        {
            Anchor = anchor;
            Id = anchor != null ? anchor.Id : string.Empty;
        }
     }
}
