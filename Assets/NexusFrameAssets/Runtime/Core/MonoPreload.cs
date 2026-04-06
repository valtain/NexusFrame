using UnityEngine;

namespace NexusFrame
{
    // Preload 씬에서 Monobehaviour 형태로 생성되는 객체
    public class MonoPreload<T> : MonoBehaviour where T : MonoPreload<T>
    {
        public static T Instance { get; private set; } = null;
        public static bool HasInstance { get; private set; } = false;

        protected static void ResetInstance()
        {
            Instance = null;
            HasInstance = false;
        }

        protected virtual void Awake()
        {
            Debug.Assert(Instance == null);
            Instance = this as T;
            HasInstance = true;
            Debug.Assert(Instance != null);
        }

        protected virtual void OnDestroy()
        {
            Debug.Assert(HasInstance);
            ResetInstance();
        }

        // Editor 에서 비정상 종료후,
        // 재시작시 Instance 가 초기화 되지 않은 경우 대비.
        // [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        // private static void Initialize() => Instance = null;
        // 그렇지만 Generic class 의 멤버는 RuntimeInitializeOnLoadMethod 사용할 수 없으므로,
        // 각 구현 class 에서 직접 호출 필요
        // 예를 들면 GameManager 가 있다면
        // public class GameManager: Preload<GameManager>
        //{
        //     [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        //     private static void Initialize() => ResetInstance();
        //}
    }
}
