using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerNormalState : PlayerState
{
    private bool _cachedQueryStartInColliders;
    [Header("Wall Interaction Variables")]
    private bool _isAgainstWall;
    private bool _isWallSliding;
    private float _wallDirection; // -1 for left wall, 1 for right wall
    private float _timeLastTouchedWall;
    private float _lastWallDirection; // Stores the direction of the wall jumped from (-1 or 1)
    private float _wallJumpLockoutEndTime; // Time when the input lockout after a wall jump ends
    private float _lastWallJumpDirection; // Stores the direction of the wall jumped from (-1 or 1)

    public override void OnEnter(StateMachineRunner runner)
    {
        base.OnEnter(runner);
        _cachedQueryStartInColliders = Physics2D.queriesStartInColliders;
        InitalizeJumpVariables();
        CheckJumpOnEnter();
        CheckCollisions();
    }

    private void InitalizeJumpVariables()
    {
        GatherInput();
        _endedJumpEarly = false;
        _timeJumpWasPressed = frameInput.JumpPressTime;
        _coyoteUsable = true;
        _bufferedJumpUsable = true;
        _canEndJumpEarly = false;
        _performedGroundJump = false; // [Modified] Reset flag
        // Initialize wall state
        _isAgainstWall = false;
        _isWallSliding = false;
        _wallDirection = 0;
        _wallJumpLockoutEndTime = -1f; // Ensure lockout is not active initially
        _lastWallJumpDirection = 0;
        _timeLastTouchedWall = -Mathf.Infinity;
    }
    private void CheckJumpOnEnter()
    {
        if (_timeJumpWasPressed + stats.jumpBuffer > Time.time)
        {
            ExecuteJump();
        }
    }
    public override void OnUpdate(StateMachineRunner runner)
    {
        base.OnUpdate(runner);
        HandleInput();
    }

    private void HandleInput()
    {
        if (frameInput.JumpDown)
        {
            _jumpToConsume = true;
            _timeJumpWasPressed = Time.time;
        }
    }

    
    public override void OnFixedUpdate(StateMachineRunner runner)
    {
        CheckCollisions();
        
        HandleDirection();
        HandleGravity();
        HandleJump();
    }
    #region Collisions
    
    private float GetFrameLeftGrounded() => PlayerController.Instance.FrameLeftGrounded;
    private bool _grounded;

    private void CheckCollisions()
    {
        Physics2D.queriesStartInColliders = false;

        // Ground and Ceiling
        bool groundHit = Physics2D.BoxCast(col.bounds.center, col.bounds.size, 0, Vector2.down, stats.grounderDistance, stats.groundLayer);
        
        float horizontalCheckOffset = col.bounds.extents.x + 0.02f; // Small offset
        Vector2 raycastOriginLeft = new Vector2(col.bounds.center.x - horizontalCheckOffset, col.bounds.center.y);
        Vector2 raycastOriginRight = new Vector2(col.bounds.center.x + horizontalCheckOffset, col.bounds.center.y);
        bool wallHitLeft = Physics2D.BoxCast(col.bounds.center, col.bounds.size, 0, Vector2.left, stats.grounderDistance, stats.groundLayer);
        bool wallHitRight = Physics2D.BoxCast(col.bounds.center, col.bounds.size, 0, Vector2.right, stats.grounderDistance, stats.groundLayer);
        
        // Determine wall state
        _isAgainstWall = (wallHitLeft || wallHitRight) && !groundHit; // Can't be against wall if grounded

        if (_isAgainstWall)
        {
            _wallDirection = wallHitRight ? -1 : 1;
            _lastWallDirection = _wallDirection;
            _timeLastTouchedWall = Time.time; // Update time when touching wall
        }
        else
        {
            _wallDirection = 0;
            // Keep _timeLastTouchedWall as is for coyote time check
        }
        // --- Landing and Leaving Ground Logic ---
        if (!_grounded && groundHit)
        {
            _grounded = true;
            _coyoteUsable = true;
            _bufferedJumpUsable = true;
            _endedJumpEarly = false;
            _canEndJumpEarly = true; // Allow early jump end after landing
            _isWallSliding = false; // Stop wall sliding when landing
            _isAgainstWall = false; // Not against wall when grounded
            _performedGroundJump = false; // [Modified] Reset ground jump flag on landing
        }
        else if (_grounded && !groundHit)
        {
            _grounded = false;
            PlayerController.Instance.UpdateFrameLeftGrounded();
            // Potentially start wall slide immediately if already against wall when leaving ground
            _isWallSliding = _isAgainstWall && rb.linearVelocity.y < 0;
        }
        // --- End Landing/Leaving ---
        if (_isAgainstWall && !_grounded && rb.linearVelocity.y <= 0)
        {
            _isWallSliding = true;
        }
        // Stop sliding if not against wall, or if grounded, or moving upwards
        else if (!_isAgainstWall || _grounded || rb.linearVelocity.y > 0)
        {
            _isWallSliding = false;
        }


        Physics2D.queriesStartInColliders = _cachedQueryStartInColliders;
    }

    #endregion


    #region Jumping

    private bool _jumpToConsume;
    private bool _bufferedJumpUsable;
    private bool _endedJumpEarly;
    private bool _canEndJumpEarly;
    private bool _coyoteUsable;
    private float _timeJumpWasPressed;
    private bool _performedGroundJump; // [Modified] New flag to track jump source

    private bool HasBufferedJump => _bufferedJumpUsable && Time.time < _timeJumpWasPressed + stats.jumpBuffer;
    private bool CanUseCoyote => _coyoteUsable && !_grounded && Time.time < GetFrameLeftGrounded() + stats.coyoteTime;
    private bool CanUseWallCoyote => !_grounded  && Time.time < _timeLastTouchedWall + stats.wallJumpCoyoteTime;
    private bool CanWallJump() => (_isAgainstWall || CanUseWallCoyote) && !_grounded && (_jumpToConsume || HasBufferedJump);
    private void HandleJump()
    {
        // Handle early jump end (if jump key released)
        if (!_endedJumpEarly && !_grounded && !frameInput.JumpHeld && rb.linearVelocity.y > 0 && _canEndJumpEarly)
        {
            _endedJumpEarly = true;
        }

        // Check if jump input needs processing
        if (!_jumpToConsume && !HasBufferedJump) return;

        // Prioritize Wall Jump, then Ground Jump, then Coyote Jump
        if (_grounded || CanUseCoyote) // Check for ground/coyote jump ONLY if not wall jumping
        {
            ExecuteJump();
        }
        else if (CanWallJump())
        {
            ExecuteWallJump();
        }

        // Consume the jump input regardless of whether a jump happened
        // (prevents accidental buffered ground jump after failing a wall jump)
        _jumpToConsume = false;
    }

    private void ExecuteJump() // Renamed from original ExecuteJump to clarify it's for ground/coyote
    {
        _endedJumpEarly = false;
        _timeJumpWasPressed = 0; // Consume buffer
        _bufferedJumpUsable = false;
        _coyoteUsable = false; // Consume coyote
        _isWallSliding = false; // Stop sliding if jumping from ground/coyote
        _canEndJumpEarly = true; // Allow early jump end for ground jumps
        _performedGroundJump = true; // [Modified] Mark this as a ground jump

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, stats.jumpPower);
    }
    private void ExecuteWallJump()
    {
        _endedJumpEarly = false;
        _timeJumpWasPressed = 0;
        _bufferedJumpUsable = false;
        _coyoteUsable = false;
        _isWallSliding = false;
        _canEndJumpEarly = true;
        _performedGroundJump = false; // [Modified] Wall jumps do not count as ground jumps

        // --- Start Changes ---
        _lastWallJumpDirection = _lastWallDirection; // STORE the wall direction we are jumping OFF
        _wallJumpLockoutEndTime = Time.time + stats.wallJumpInputLockoutDuration; // START the input lockout
        // --- End Changes ---

        float horizontalInput = frameInput.Move.x;
        bool hasHorizontalInput = Mathf.Abs(horizontalInput) > 0.1f;
        float horizontalForce = hasHorizontalInput ? stats.wallJumpForceHorizontalWithInput : stats.wallJumpForceHorizontalBase;
        float forceDirection = _lastWallJumpDirection; // Use the stored direction

        rb.linearVelocity = new Vector2(horizontalForce * forceDirection, stats.wallJumpForceVertical);

    }
    #endregion

    #region Horizontal

    private void HandleDirection()
    {
        float horizontalInput = frameInput.Move.x; // Read raw input
        bool lockoutActive = Time.time < _wallJumpLockoutEndTime;

        if (lockoutActive)
        {
            // If lockout is active AND input is towards the wall we jumped from, ignore input
            if (!Mathf.Approximately(Mathf.Sign(horizontalInput), _lastWallJumpDirection))
            {
                horizontalInput = 0; 
            }
        }

        float targetXVelocity = horizontalInput * stats.maxSpeed;

        if (horizontalInput == 0)
        {
            var deceleration = _grounded ? stats.groundDeceleration : stats.airDeceleration;
            rb.linearVelocity = new Vector2(Mathf.MoveTowards(rb.linearVelocity.x, 0, deceleration * Time.fixedDeltaTime), rb.linearVelocity.y);
        }
        // [FIXED] Symmetrical check for exceeding max speed (checks absolute value)
        // AND checks if input is trying to accelerate further in that direction.
        else if (Mathf.Abs(rb.linearVelocity.x) > stats.maxSpeed && Mathf.Sign(horizontalInput) == Mathf.Sign(rb.linearVelocity.x))
        {
            // [Celeste Logic] Retain momentum but apply slight drag (Air Deceleration).
            // Do NOT clamp instantly to maxSpeed, and do NOT apply acceleration.
            // MoveTowards 'targetXVelocity' (which is lower than current speed) using airDeceleration handles the soft cap.
            rb.linearVelocity = new Vector2(Mathf.MoveTowards(rb.linearVelocity.x, targetXVelocity, stats.extraAirDeceleration * Time.fixedDeltaTime), rb.linearVelocity.y);
        }
        else
        {
            // Standard Movement (Acceleration)
            // Applies when speed < maxSpeed OR input is opposite to current velocity (Turn).
            rb.linearVelocity = new Vector2(Mathf.MoveTowards(rb.linearVelocity.x, targetXVelocity, stats.acceleration * Time.fixedDeltaTime), rb.linearVelocity.y);
        }
    }

    #endregion

    #region Gravity

    private void HandleGravity()
    {
        // Apply wall slide gravity if applicable
        if (_isWallSliding) // Use the dedicated flag
        {
            // Clamp downward velocity to wall slide speed
            if (rb.linearVelocity.y < -stats.wallSlideSpeed)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, -stats.wallSlideSpeed);
            }
            // Optional: Apply a slight downward force if stationary to initiate slide
            else if (rb.linearVelocity.y > -stats.wallSlideSpeed)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.MoveTowards(rb.linearVelocity.y, -stats.maxFallSpeed, stats.fallAcceleration * .2f * Time.fixedDeltaTime));
            }
        }
        // Apply normal gravity if not wall sliding
        else if (_grounded && rb.linearVelocity.y <= 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, stats.groundingForce);
        }
        else // Normal Air Gravity
        {
            var inAirGravity = stats.fallAcceleration;

            // [Modified] Apply jump cut gravity modifier ONLY if not bouncing upwards from something else AND performed a ground jump
            if (_endedJumpEarly && rb.linearVelocity.y > 0 && _performedGroundJump)
            {
                inAirGravity *= stats.jumpEndEarlyGravityModifier;
            }

            // Apply gravity, clamping to max fall speed
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.MoveTowards(rb.linearVelocity.y, -stats.maxFallSpeed, inAirGravity * Time.fixedDeltaTime));
        }
    }

    #endregion
}
