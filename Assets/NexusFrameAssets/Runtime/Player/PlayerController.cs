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
    public override bool IsMoving => _isMoving;

    [SerializeField]
    private CharacterController _characterController;

    private Transform _cachedTransform;

    private Vector3 _rawInput;
    private Vector3 _input;
    private Vector3 _moveXzVector;

    [SerializeField]
    private CameraRelativeInputFrameResolver _inputFrameResolver = new();

    private bool _isMoving;

    private void Awake()
    {
        _cachedTransform = this.transform;
    }

    private void OnEnable()
    {
        ResetParams();
        _inputFrameResolver.Reset();
    }

    private void Update()
    {
        var deltaTime = Time.deltaTime;
        var previousRawInput = _rawInput;
        _rawInput = new Vector3(MoveX.Value, 0, MoveZ.Value);

        bool inputDirectionChanged = Vector3.Dot(_rawInput, previousRawInput) < 0.8f;
        Quaternion cameraRotation = SceneDirector.Instance.MainCameraTransform.rotation;
        var inputFrame = _inputFrameResolver.Update(
            cameraRotation,
            _cachedTransform.rotation * Vector3.up,
            inputDirectionChanged,
            deltaTime);

        _input = inputFrame * _rawInput;
        if (_input.sqrMagnitude > 1f)
        {
            _input.Normalize();
        }

        var damp = Damper.Damp(1, damping, deltaTime);
        var upDirection = Vector3.up; // world 기준

        // 이동량 계산
        UpdateMoveXzVector(damp);
        _isMoving = 1.0e-3f < _moveXzVector.sqrMagnitude;

        // 이동량 적용
        {
            _characterController.Move(_moveXzVector * deltaTime);
        }

        // 이동량 기반 주시 방향 조정
        UpdateRotation(inputFrame, upDirection, damp);
    }

    private void UpdateRotation(Quaternion inputFrame, Vector3 upDirection, float damp)
    {
        if (!_isMoving)
        {
            return;
        }

        var rotationLast = _cachedTransform.rotation;

        // Player 기준
        var inputForward = inputFrame * Vector3.forward;
        var isBackwardMove = Vector3.Dot(inputForward, _moveXzVector) < 0f;
        var idealRotation = Quaternion.LookRotation(
            isBackwardMove ? -_moveXzVector : _moveXzVector,
            upDirection);

        _cachedTransform.rotation = Quaternion.Slerp(rotationLast, idealRotation, damp);
    }

    private void UpdateMoveXzVector(float damp)
    {
        var lastMoveXzVector = _moveXzVector;
        var idealMoveXzVector = _input * Speed;

        var isBigChange = 100f < Vector3.Angle(lastMoveXzVector, idealMoveXzVector);

        _moveXzVector =
            isBigChange
            ? Vector3.Lerp(lastMoveXzVector, idealMoveXzVector, damp)
            : Vector3.Slerp(lastMoveXzVector, idealMoveXzVector, damp);
    }

    private void ResetParams()
    {
        _moveXzVector = Vector3.zero;
        _input = Vector3.zero;
        _rawInput = Vector3.zero;
        _isMoving = false;
    }
}
}
