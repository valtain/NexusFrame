using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;

namespace NexusFrame
{
/// <summary>
/// 플레이어 컨트롤러의 공통 입력 정의와 인터페이스 계약을 담당하는 추상 기반 클래스.
/// <para>
/// <b>역할 분담:</b>
/// <list type="bullet">
///   <item><description>
///     <b>PlayerControllerBase (이 클래스)</b> — Cinemachine <see cref="IInputAxisOwner"/> 구현을 통해
///     MoveX/MoveZ/Jump/Sprint 입력 축을 선언하고, Speed/SprintSpeed 등 공통 파라미터와
///     <see cref="Landed"/> 이벤트를 정의한다. 입력 수신 이외의 물리·이동 로직은 포함하지 않는다.
///   </description></item>
///   <item><description>
///     <b>구현 클래스 (ex: PlayerController)</b> — <see cref="CharacterController"/>를 이용한 실제 이동 연산,
///     카메라 상대 입력 변환(<c>CameraRelativeInputFrameResolver</c>), 회전 처리 등
///     월드 내 구체적인 이동 동작을 구현한다.
///   </description></item>
/// </list>
/// </para>
/// </summary>
public abstract class PlayerControllerBase : MonoBehaviour, IInputAxisOwner
{
    [Header("Input parameters")]
    public float Speed = 1f;
    public float SprintSpeed = 4f;
    public float JumpSpeed = 4f;
    public float SprintJumpSpeed = 6f;

    public float damping = 0.5f;

    [Header("Input Axes")]
    public InputAxis MoveX = InputAxis.DefaultMomentary;
    public InputAxis MoveZ = InputAxis.DefaultMomentary;
    public InputAxis Jump = InputAxis.DefaultMomentary;
    public InputAxis Sprint = InputAxis.DefaultMomentary;

    [Header("Events")]
    public UnityEvent Landed = new ();

    void IInputAxisOwner.GetInputAxes(List<IInputAxisOwner.AxisDescriptor> axes)
    {
        axes.Add( new () { DrivenAxis = () => ref MoveX, Name = "Move X", Hint = IInputAxisOwner.AxisDescriptor.Hints.X});
        axes.Add( new () { DrivenAxis = () => ref MoveZ, Name = "Move Y", Hint = IInputAxisOwner.AxisDescriptor.Hints.Y});
        axes.Add( new () { DrivenAxis = () => ref Jump, Name = "Jump"});
        axes.Add( new () { DrivenAxis = () => ref Sprint, Name = "Sprint"});
    }

    protected virtual void OnValidate()
    {
        MoveX.Validate();
        MoveZ.Validate();
        Jump.Validate();
        Sprint.Validate();
    }

    public abstract bool IsMoving {get;}
}
}
