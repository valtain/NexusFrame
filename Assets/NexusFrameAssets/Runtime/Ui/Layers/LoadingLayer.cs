using Cysharp.Threading.Tasks;
using UnityEngine;

namespace NexusFrame
{
    /// <summary>
    /// 비동기 작업 중 입력을 차단하는 레이어.
    /// 입력 차단은 BgCover Canvas의 Graphic이 담당한다.
    /// <see cref="Transition"/>과 동일한 ref-count Begin/End 패턴을 사용한다.
    /// </summary>
    public class LoadingLayer : MonoBehaviour
    {
        [field: SerializeField] public Canvas Canvas { get; private set; }

        public int LoadingCount { get; private set; }
        public static bool IsLoading => UiSystem.HasInstance && UiSystem.Instance.Loading.LoadingCount > 0;

        public async UniTask Begin()
        {
            ++LoadingCount;
            if (LoadingCount >= 2) return;

            UiSystem.Instance.BgCover.Request(BgCoverPriority.Loading);
            await UniTask.Yield();
        }

        public async UniTask End()
        {
            --LoadingCount;
            if (LoadingCount >= 1) return;

            UiSystem.Instance.BgCover.Release(BgCoverPriority.Loading);
            await UniTask.Yield();
        }

        public async UniTask<LoadingScope> Scope()
        {
            await Begin();
            return new LoadingScope();
        }

        public readonly struct LoadingScope
        {
            public UniTask DisposeAsync() => UiSystem.Instance.Loading.End();
        }
    }
}
