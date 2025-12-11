using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerGrabState : PlayerState
{
    [Header("Holding Settings")]
    [SerializeField] private PlayerStats customStats; // Assign modified stats here

    [SerializeField] private PlayerState normalState;
    // Use custom stats if assigned, otherwise fallback to default
    private PlayerStats CurrentStats => customStats ? customStats : PlayerStats.Instance;

    private GrabbableObject _currentObject;
    private Transform _holdPoint;
    private float _timeEntered;

    // Movement Variables (Replicated from PlayerNormalState)
    private bool _cachedQueryStartInColliders;
    private bool _grounded;
    
    // Jump Variables
    private bool _jumpToConsume;
    private bool _bufferedJumpUsable;
    private bool _endedJumpEarly;
    private bool _canEndJumpEarly;
    private bool _coyoteUsable;
    private float _timeJumpWasPressed;

    public void Initialize(GrabbableObject obj, Transform point)
    {
        _currentObject = obj;
        _holdPoint = point;
    }

    public override void OnEnter(StateMachineRunner runner)
    {
        base.OnEnter(runner);
        
        if (_currentObject == null)
        {
            runner.ChangeState(GetComponent<PlayerNormalState>() ?? runner.GetComponentInChildren<PlayerNormalState>() ?? FindFirstObjectByType<PlayerNormalState>());
            return;
        }

        // Setup Object
        _currentObject.Grab();
        _currentObject.transform.SetParent(_holdPoint);
        _currentObject.transform.localPosition = Vector2.zero;

        _timeEntered = Time.time;
        _cachedQueryStartInColliders = Physics2D.queriesStartInColliders;
        InitializeJumpVariables();
    }

    public override void OnExit(StateMachineRunner runner)
    {
        // Fail-safe: if we exit state without throwing, drop the object
        if (_currentObject != null && _currentObject.transform.parent == _holdPoint)
        {
            _currentObject.Throw(Vector2.zero);
        }
        base.OnExit(runner);
    }

    public override void OnUpdate(StateMachineRunner runner)
    {
        base.OnUpdate(runner);
        
        // Handle Throwing (Prevent throwing on the same frame as grabbing)
        if (Time.time > _timeEntered + 0.1f)
        {
            if (frameInput.DashDown) // Press Sprint to Throw
            {
                ThrowObject();
                // Find and return to Normal State
                ReturnToNormalState(runner);
                return;
            }
        }

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

    private void ThrowObject()
    {
        if (_currentObject == null) return;

        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.value);
        Vector2 playerPos = controller.transform.position;
        Vector2 dir = (mousePos - playerPos).normalized;

        _currentObject.Throw(dir);
        _currentObject = null;
    }

    private void ReturnToNormalState(StateMachineRunner runner)
    {
        runner.ChangeState(normalState);
    }

    // --- LOGIC REPLICATED FROM PlayerNormalState (Using CurrentStats) ---

    private void InitializeJumpVariables()
    {
        _endedJumpEarly = false;
        _coyoteUsable = true;
        _bufferedJumpUsable = true;
        _canEndJumpEarly = false;
    }

    private void CheckCollisions()
    {
        Physics2D.queriesStartInColliders = false;
        
        // Ground Check
        bool groundHit = Physics2D.BoxCast(col.bounds.center, col.bounds.size, 0, Vector2.down, CurrentStats.grounderDistance, CurrentStats.groundLayer);

        if (!_grounded && groundHit)
        {
            _grounded = true;
            _coyoteUsable = true;
            _bufferedJumpUsable = true;
            _endedJumpEarly = false;
            _canEndJumpEarly = true;
            controller.UpdateFrameLeftGrounded();
        }
        else if (_grounded && !groundHit)
        {
            _grounded = false;
            controller.UpdateFrameLeftGrounded();
        }

        Physics2D.queriesStartInColliders = _cachedQueryStartInColliders;
    }

    private void HandleDirection()
    {
        float horizontalInput = frameInput.Move.x;
        
        if (horizontalInput == 0)
        {
            var deceleration = _grounded ? CurrentStats.groundDeceleration : CurrentStats.airDeceleration;
            rb.linearVelocity = new Vector2(Mathf.MoveTowards(rb.linearVelocity.x, 0, deceleration * Time.fixedDeltaTime), rb.linearVelocity.y);
        }
        else
        {
            rb.linearVelocity = new Vector2(Mathf.MoveTowards(rb.linearVelocity.x, horizontalInput * CurrentStats.maxSpeed, CurrentStats.acceleration * Time.fixedDeltaTime), rb.linearVelocity.y);
        }
    }

    private void HandleGravity()
    {
        if (_grounded && rb.linearVelocity.y <= 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, CurrentStats.groundingForce);
        }
        else
        {
            var inAirGravity = CurrentStats.fallAcceleration;
            if (_endedJumpEarly && rb.linearVelocity.y > 0)
            {
                inAirGravity *= CurrentStats.jumpEndEarlyGravityModifier;
            }
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.MoveTowards(rb.linearVelocity.y, -CurrentStats.maxFallSpeed, inAirGravity * Time.fixedDeltaTime));
        }
    }

    private void HandleJump()
    {
        if (!_endedJumpEarly && !_grounded && !frameInput.JumpHeld && rb.linearVelocity.y > 0 && _canEndJumpEarly)
        {
            _endedJumpEarly = true;
        }

        if (!_jumpToConsume && !(_bufferedJumpUsable && Time.time < _timeJumpWasPressed + CurrentStats.jumpBuffer)) return;

        if (_grounded || (_coyoteUsable && !_grounded && Time.time < controller.FrameLeftGrounded + CurrentStats.coyoteTime))
        {
            ExecuteJump();
        }

        _jumpToConsume = false;
    }

    private void ExecuteJump()
    {
        _endedJumpEarly = false;
        _timeJumpWasPressed = 0;
        _bufferedJumpUsable = false;
        _coyoteUsable = false;
        _canEndJumpEarly = true;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, CurrentStats.jumpPower);
    }
}