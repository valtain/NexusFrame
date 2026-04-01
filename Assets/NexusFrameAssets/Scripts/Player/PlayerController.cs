using NexusFrame;
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
    private Vector3 upDirection;
    private Vector3 moveXzVector;

    [SerializeField]
    private CameraRelativeInputFrameResolver inputFrameResolver = new();


    private bool isMoving;

    private void Awake()
    {
        cachedTransform = this.transform;
    }

    private void OnEnable()
    {
        ResetParams();
        inputFrameResolver.Reset();
    }

    private void Update()
    {
        var deltaTime = Time.deltaTime;
        var rawInput_asis = rawInput;
        rawInput = new Vector3(MoveX.Value, 0, MoveZ.Value);

        bool inputDirectionChanged = Vector3.Dot( rawInput, rawInput_asis) < 0.8f;
        Quaternion camerRotation = SceneDirector.Instance.MainCameraTransform.rotation;
        var inputFrame = inputFrameResolver.Update(
            camerRotation,
            cachedTransform.rotation * Vector3.up,
            inputDirectionChanged,
            deltaTime);

        input = inputFrame * rawInput;
        if (input.sqrMagnitude > 1f)
        {
            input.Normalize();
        }

        upDirection = Vector3.up; // world 기준
        var damp = Damper.Damp(1, damping, deltaTime);

        // 이동량 계산
        {
            var lastMoveXzVector = moveXzVector;
            var idealMoveXzVector = input * Speed;
            var moveDamp = damp;

            var isBigChange = 100f < Vector3.Angle(lastMoveXzVector, idealMoveXzVector);

            moveXzVector =
                isBigChange
                ? Vector3.Lerp(lastMoveXzVector, idealMoveXzVector, moveDamp)
                : Vector3.Slerp(lastMoveXzVector, idealMoveXzVector, moveDamp);
            isMoving = 1.0e-3f < moveXzVector.sqrMagnitude;
        }

        // 이동량 적용
        {
            characterController.Move(moveXzVector*deltaTime);
        }

        // 이동량 기반 주시 방향 조정
        if (isMoving)
        {
            var rotationDamp = damp;
            var rotationLast = cachedTransform.rotation;

            // Player 기준
            var inputForward = inputFrame * Vector3.forward;
            var isBackwardMove = Vector3.Dot(inputForward, moveXzVector) < 0f;
            var idealRotation = Quaternion.LookRotation(
                isBackwardMove ? -moveXzVector: moveXzVector,
                upDirection);

            var rotation = Quaternion.Slerp(rotationLast, idealRotation, rotationDamp);
            cachedTransform.rotation = rotation;
        }
    }

    private void ResetParams()
    {
        upDirection = Vector3.up;
        moveXzVector = Vector3.zero;
        input = Vector3.zero;
        rawInput = Vector3.zero;
        isMoving = false;
    }

}
