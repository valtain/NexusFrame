using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;

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
