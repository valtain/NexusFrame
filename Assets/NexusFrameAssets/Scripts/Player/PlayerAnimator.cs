using UnityEngine;

/// <summary>
/// 플레이어 오브젝트의 실제 위치 변화를 기반으로 애니메이션 파라미터를 구동하는 컴포넌트.
/// <para>
/// - <see cref="PlayerController"/>의 이동 로직과 직접 결합하지 않고, LateUpdate에서 프레임 간 위치 차이를
///   로컬 좌표계 속도로 변환하여 애니메이션 파라미터를 갱신한다.
/// - Animator 파라미터 <c>Speed</c>(전후), <c>Direction</c>(좌우)에 로컬 이동 방향을 전달한다.
/// </para>
/// </summary>
public class PlayerAnimator : MonoBehaviour
{
    [field: SerializeField]
    public Animator Animator {get; private set;} = default;

    private Vector3 _lastPosition = default;
    private Transform _cachedTransform = default;

    private AnimParams _animParams = AnimParams.@default;

    protected struct AnimParams
    {
        public Vector3 Direction;

        public static readonly AnimParams @default = new AnimParams()
        {
            Direction = Vector3.zero
        };
    }

    private void Start()
    {
        _cachedTransform = gameObject.transform;
        _lastPosition = _cachedTransform.position;
        if (Animator == null)
        {
            Animator = GetComponent<Animator>();
        }
    }

    private void LateUpdate()
    {
        var pos = _cachedTransform.position;
        var movement = pos - _lastPosition;
        var localMovement = Quaternion.Inverse(_cachedTransform.rotation) * movement / Time.deltaTime;
        _lastPosition = pos;
        UpdateAnimationState(localMovement);
        UpdateAnimation();
    }

    private void UpdateAnimationState(Vector3 movement)
    {
        var speed = movement.magnitude;

        _animParams.Direction = speed > 0.1f ? movement/speed: Vector3.zero;
    }

    private void UpdateAnimation()
    {
        Animator.SetFloat("Speed", _animParams.Direction.z);
        Animator.SetFloat("Direction", _animParams.Direction.x);
    }
}
