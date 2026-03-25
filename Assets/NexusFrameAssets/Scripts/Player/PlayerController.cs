using Unity.Cinemachine;
using UnityEngine;

public class PlayerController : PlayerControllerBase
{
    public override bool IsMoving => isMoving;

    [SerializeField]
    private CharacterController characterController;

    private Transform cachedTransform;

    private Vector3 rawInput;
    private Vector3 input;
    private Vector3 lookUpDirection;
    private Vector3 moveXzVector;


    private bool isMoving;

    private void Awake()
    {
        cachedTransform = this.transform;
    }

    private void OnEnable()
    {
        ResetParams();
    }

    private void Update()
    {
        var deltaTime = Time.deltaTime;
        rawInput = new Vector3(MoveX.Value, 0, MoveZ.Value);
        Quaternion inputFrame = cachedTransform.rotation;

        input = inputFrame * rawInput;
        if (input.sqrMagnitude > 1f)
        {
            input.Normalize();
        }

        lookUpDirection = cachedTransform.up;
        var damp = Damper.Damp(1, damping, deltaTime);

        // 이동량 계산
        {
            var lastMoveXzVector = moveXzVector;
            var idealMoveXzVector = input * (Speed * deltaTime);
            var moveDamp = damp;

            var isSteepChange = 100f < Vector3.Angle(lastMoveXzVector, idealMoveXzVector);

            moveXzVector =
                isSteepChange
                ? Vector3.Lerp(lastMoveXzVector, idealMoveXzVector, moveDamp)
                : Vector3.Slerp(lastMoveXzVector, idealMoveXzVector, moveDamp);
            isMoving = 1.0e-3f < moveXzVector.sqrMagnitude;
        }

        // 이동량 적용
        {
            characterController.Move(moveXzVector);
        }

        // 이동량 기반 주시 방향 조정
        if (isMoving)
        {
            var lookDamp = damp;
            var lookLast = cachedTransform.rotation;
            var inputForward = inputFrame * Vector3.forward;
            var isBackwardMove = Vector3.Dot(inputForward, moveXzVector) < 0f;
            var idealLook = Quaternion.LookRotation(
                isBackwardMove ? -moveXzVector: moveXzVector,
                lookUpDirection);

            var look = Quaternion.Slerp(lookLast, idealLook, lookDamp);
            cachedTransform.rotation = look;
        }
    }

    private void ResetParams()
    {
        lookUpDirection = Vector3.up;
        moveXzVector = Vector3.zero;
        input = Vector3.zero;
        rawInput = Vector3.zero;
        isMoving = false;
    }

}
