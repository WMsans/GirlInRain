using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameInputManager : MonoSingleton<GameInputManager>
{
    private InputSystem_Actions _inputSystemActions;


    public FrameInput CurrentFrameInput { get; private set; } = new()
    {
        JumpDown = false,
        JumpHeld = false,
        JumpPressTime = -Mathf.Infinity,
        DashDown = false,
        DashPressTime = -Mathf.Infinity,
        Move = Vector2.zero,
        LastMove = Vector2.zero,
        InteractDown = false,
    };
    protected override void Awake()
    {
        base.Awake();
        _inputSystemActions = new InputSystem_Actions();
    }

    private void OnEnable()
    {
        _inputSystemActions.Player.Jump.Enable();
        _inputSystemActions.Player.Sprint.Enable();
        _inputSystemActions.Player.Move.Enable();
        _inputSystemActions.Player.Interact.Enable();
    }
    private void OnDisable()
    {
        _inputSystemActions.Player.Jump.Disable();
        _inputSystemActions.Player.Sprint.Disable();
        _inputSystemActions.Player.Move.Disable();
        _inputSystemActions.Player.Interact.Disable();
    }

    private void Update()
    {
        CurrentFrameInput = new FrameInput
        {
            JumpDown = _inputSystemActions.Player.Jump.WasPressedThisFrame(),
            JumpHeld = _inputSystemActions.Player.Jump.IsPressed(),
            JumpPressTime = _inputSystemActions.Player.Jump.WasPressedThisFrame() ? Time.time : CurrentFrameInput.JumpPressTime,
            DashDown = _inputSystemActions.Player.Sprint.WasPressedThisFrame(),
            DashPressTime = _inputSystemActions.Player.Sprint.WasPressedThisFrame() ? Time.time : CurrentFrameInput.DashPressTime,
            Move = _inputSystemActions.Player.Move.ReadValue<Vector2>(),
            LastMove = _inputSystemActions.Player.Move.ReadValue<Vector2>().sqrMagnitude < 0.1f ? CurrentFrameInput.LastMove : _inputSystemActions.Player.Move.ReadValue<Vector2>(),
            InteractDown = _inputSystemActions.Player.Interact.WasPressedThisFrame(),
        };
    }
}
