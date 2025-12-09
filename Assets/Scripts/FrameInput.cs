using UnityEngine;

public struct FrameInput
{
    public bool JumpDown;
    public bool JumpHeld;
    public float JumpPressTime;
    public bool DashDown;
    public float DashPressTime;
    public Vector2 Move;
    public Vector2 LastMove;
    public bool InteractDown;
}
