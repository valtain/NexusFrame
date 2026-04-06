using Unity.Cinemachine;
using UnityEngine;

namespace NexusFrame
{
/// <summary>
/// <see cref="PlayerControllerBase"/>의 기본 구현 클래스. 지상 환경에서의 이동·회전을 처리한다.
/// <para>
/// - 입력 값을 카메라 상대 좌표계로 변환(<c>CameraRelativeInputFrameResolver</c>)하여 이동 방향을 결정한다.
/// - <see cref="CharacterController"/>로 실제 이동을 적용하며, Slerp/Lerp 댐핑으로 움직임을 부드럽게 처리한다.
/// - 이동 방향 기반으로 캐릭터 회전을 갱신하며, 후진 입력 시 자동으로 반전 회전을 적용한다.
/// </para>
/// </summary>
public class PlayerController : PlayerControllerBase
{
    public override bool IsMoving => isMoving;

    [SerializeField]
    private CharacterController characterController;

    private Transform cachedTransform;

    private Vector3 rawInput;
    private Vector3 input;
    private Vector3 moveXzVector;
    private Vector3 upDirection; // Jump

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

        bool inputDirectionChanged = Vector3.Dot(rawInput, rawInput_asis) < 0.8f;
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

        var damp = Damper.Damp(1, damping, deltaTime);
        var upDirection = Vector3.up; // world 기준

        // 이동량 계산
        UpdateMoveXzVector(damp);
        isMoving = 1.0e-3f < moveXzVector.sqrMagnitude;

        // 이동량 적용
        {
            characterController.Move(moveXzVector * deltaTime);
        }

        // 이동량 기반 주시 방향 조정
        UpdateRotation(inputFrame, upDirection, damp);
    }

    private void UpdateRotation(Quaternion inputFrame, Vector3 upDirection, float damp)
    {
        if (!isMoving)
        {
            return;
        }

        var rotationDamp = damp;
        var rotationLast = cachedTransform.rotation;

        // Player 기준
        var inputForward = inputFrame * Vector3.forward;
        var isBackwardMove = Vector3.Dot(inputForward, moveXzVector) < 0f;
        var idealRotation = Quaternion.LookRotation(
            isBackwardMove ? -moveXzVector : moveXzVector,
            upDirection);

        var rotation = Quaternion.Slerp(rotationLast, idealRotation, rotationDamp);
        cachedTransform.rotation = rotation;
    }

    private void UpdateMoveXzVector(float damp)
    {
        var lastMoveXzVector = moveXzVector;
        var idealMoveXzVector = input * Speed;
        var moveDamp = damp;

        var isBigChange = 100f < Vector3.Angle(lastMoveXzVector, idealMoveXzVector);

        moveXzVector =
            isBigChange
            ? Vector3.Lerp(lastMoveXzVector, idealMoveXzVector, moveDamp)
            : Vector3.Slerp(lastMoveXzVector, idealMoveXzVector, moveDamp);
    }

    private void ResetParams()
    {
        moveXzVector = Vector3.zero;
        input = Vector3.zero;
        rawInput = Vector3.zero;
        isMoving = false;
    }
}
}
