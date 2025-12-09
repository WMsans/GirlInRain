using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class PlayerState : State
{
    protected PlayerStats stats => PlayerStats.Instance;
    protected Rigidbody2D rb;
    protected Animator animator;
    protected CapsuleCollider2D col;
    protected PlayerController controller;
    protected FrameInput frameInput;

    public override void OnEnter(StateMachineRunner runner)
    {
        controller = PlayerController.Instance;
        rb = controller.rb;
        animator = controller.animator;
        col = controller.col as CapsuleCollider2D;
        GatherInput();
    }
    public override void OnUpdate(StateMachineRunner runner)
    {
        GatherInput();
    }
    protected void GatherInput()
    {
        frameInput = GameInputManager.Instance.CurrentFrameInput;
    }
}
