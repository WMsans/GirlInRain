using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "New Player Stats", menuName = "Custom Assets/Player Controller/Player Stats", order = 1)]
public class PlayerStats : ScriptableObjectSingleton<PlayerStats>
{
    [FormerlySerializedAs("GroundLayer")] [Header("LAYERS")] [Tooltip("Set this to the layer your player is collided on")]
    public LayerMask groundLayer;
    
    [Tooltip("Set this to the layer your walls are on")]
    public LayerMask wallLayer; // Set this to the layer your walls are on
    
    [Tooltip("Set this to the layer your water is on")]
    public LayerMask waterLayerMask;

    [FormerlySerializedAs("MaxSpeed")] [Header("MOVEMENT")] [Tooltip("The top horizontal movement speed")]
    public float maxSpeed = 14;

    [FormerlySerializedAs("Acceleration")] [Tooltip("The player's capacity to gain horizontal speed")]
    public float acceleration = 120;

    [FormerlySerializedAs("GroundDeceleration")] [Tooltip("The pace at which the player comes to a stop")]
    public float groundDeceleration = 60;

    [FormerlySerializedAs("AirDeceleration")] [Tooltip("Deceleration in air only after stopping input mid-air")]
    public float airDeceleration = 30;

    [FormerlySerializedAs("GroundingForce")] [Tooltip("A constant downward force applied while grounded. Helps on slopes"), Range(0f, -10f)]
    public float groundingForce = -1.5f;

    [FormerlySerializedAs("GrounderDistance")] [Tooltip("The detection distance for grounding and roof detection"), Range(0f, 0.5f)]
    public float grounderDistance = 0.05f;

    [FormerlySerializedAs("JumpPower")] [Header("JUMP")] [Tooltip("The immediate velocity applied when jumping")]
    public float jumpPower = 36;

    [FormerlySerializedAs("MaxFallSpeed")] [Tooltip("The maximum vertical movement speed")]
    public float maxFallSpeed = 40;

    [FormerlySerializedAs("FallAcceleration")] [Tooltip("The player's capacity to gain fall speed. a.k.a. In Air Gravity")]
    public float fallAcceleration = 110;

    [FormerlySerializedAs("JumpEndEarlyGravityModifier")] [Tooltip("The gravity multiplier added when jump is released early")]
    public float jumpEndEarlyGravityModifier = 3;

    [FormerlySerializedAs("CoyoteTime")] [Tooltip("The time before coyote jump becomes unusable. Coyote jump allows jump to execute even after leaving a ledge")]
    public float coyoteTime = .15f;

    [FormerlySerializedAs("JumpBuffer")] [Tooltip("The amount of time we buffer a jump in seconds. This allows jump input before actually hitting the ground")]
    public float jumpBuffer = .2f;
    
    [Header("Wall Interaction")]
    public float wallCheckDistance = 0.1f; // How far out to check for walls
    public float wallSlideSpeed = 2f; // Max speed when sliding down a wall
    public float wallJumpCoyoteTime = 0.15f; // Time after leaving wall to still wall jump
    public float wallJumpInputLockoutDuration = 0.15f; // Duration (seconds) to ignore input towards the wall after jumping

    [Header("Wall Jump Forces")]
    public float wallJumpForceVertical = 15f; // Vertical force of the wall jump
    public float wallJumpForceHorizontalBase = 10f; // Horizontal force when jumping with no directional input
    public float wallJumpForceHorizontalWithInput = 12f; // Horizontal force when jumping with directional input (towards or away)
    public float wallJumpInputLeeway = 0.1f; // Small time window to allow input slightly before/after jump press for bonus force
    
    [Header("Dash-Related, Abandoned")]
    public int maxAirDashes = 1;
    public float dashCooldown = 0.25f;
    public float anchorDashDetectionAngle = 45f;
    public float anchorDashMaxDistance = 10f;
    public float anchorDashCooldown = 0.8f;
    public float anchorDashBufferTime = 0.2f;
}
