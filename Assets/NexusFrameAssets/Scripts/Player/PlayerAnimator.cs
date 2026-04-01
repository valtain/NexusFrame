using UnityEngine;

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
