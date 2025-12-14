using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerSwimGrabState : PlayerState
{
    [Header("State Transitions")]
    [Tooltip("The state to switch to when exiting water while holding an object.")]
    [SerializeField] private PlayerGrabState landGrabState;
    [Tooltip("The state to switch to if the object is thrown or dropped.")]
    [SerializeField] private PlayerSwimState normalSwimState;
    
    [Header("Settings")]
    [SerializeField] private PlayerStats customStats;
    private PlayerStats CurrentStats => customStats ? customStats : PlayerStats.Instance;

    // --- Object Holding Variables ---
    private GrabbableObject _currentObject;
    private Transform _holdPoint;

    // --- Swim/Stamina Variables ---
    private float _currentStamina;
    private SpriteRenderer _spriteRenderer;
    private Color _defaultColor;

    /// <summary>
    /// Call this before changing state to SwimGrab to pass the held object.
    /// </summary>
    public void Initialize(GrabbableObject obj, Transform point)
    {
        _currentObject = obj;
        _holdPoint = point;
    }

    public override void OnEnter(StateMachineRunner runner)
    {
        base.OnEnter(runner);

        // Validation: If we entered without an object, revert to normal swim
        if (_currentObject == null)
        {
            // Fallback to finding the normal swim state if not assigned
            State nextState = normalSwimState ? (State)normalSwimState : GetComponent<PlayerNormalState>();
            runner.ChangeState(nextState);
            return;
        }

        // 1. Setup Object (Parenting & Physics)
        _currentObject.Grab();
        _currentObject.transform.SetParent(_holdPoint);
        _currentObject.transform.localPosition = Vector2.zero;

        // 2. Setup Swim Physics
        rb.gravityScale = 0; // Disable gravity for buoyancy
        rb.linearVelocity *= 0.25f; // Dampen entry velocity

        // 3. Setup Stamina Visuals
        if (!_spriteRenderer) _spriteRenderer = controller.GetComponentInChildren<SpriteRenderer>();
        if (_spriteRenderer) _defaultColor = _spriteRenderer.color;
        
        _currentStamina = CurrentStats.maxSwimStamina;
    }

    public override void OnExit(StateMachineRunner runner)
    {
        // Restore Physics
        rb.gravityScale = 1; 
        if (_spriteRenderer) _spriteRenderer.color = _defaultColor;

        // Handle Object Drop / Clean up
        // If the object is still parented to us (meaning we didn't hand it off to another state),
        // we must throw it to prevent it from getting stuck on the player.
        if (_currentObject != null && _currentObject.transform.parent == _holdPoint)
        {
             _currentObject.Throw(Vector2.zero);
        }

        base.OnExit(runner);
    }

    public override void OnUpdate(StateMachineRunner runner)
    {
        base.OnUpdate(runner);
        
        HandleStaminaVisuals();

        // THROW INPUT (Dash/Sprint key)
        if (frameInput.DashDown) 
        {
            ThrowObject();
            runner.ChangeState(normalSwimState);
            return;
        }

        // JUMP INPUT (Swim Jump)
        if (frameInput.JumpDown)
        {
            PerformSwimJump();
        }
    }

    public override void OnFixedUpdate(StateMachineRunner runner)
    {
        // EXIT WATER CHECK
        if (!IsInWater())
        {
            TransitionToLandGrab(runner);
            return;
        }

        HandleMovement();
        HandleStamina();
    }

    // --- Helper Methods ---

    private void TransitionToLandGrab(StateMachineRunner runner)
    {
        // Find the land grab state if not assigned in inspector
        if (landGrabState == null) 
            landGrabState = GetComponent<PlayerGrabState>();

        if (landGrabState != null)
        {
            // Handover: Initialize the land state with our current object
            landGrabState.Initialize(_currentObject, _holdPoint);
            
            // Set local reference to null so OnExit() knows NOT to Throw/Drop the object
            // This effectively "passes" ownership to the next state.
            _currentObject = null;
            
            runner.ChangeState(landGrabState);
        }
        else
        {
            // Fallback: If no land state found, drop object and go normal
            if (_currentObject != null) 
            {
                _currentObject.Throw(Vector2.zero);
                _currentObject = null;
            }
            runner.ChangeState(GetComponent<PlayerNormalState>());
        }
    }

    private void ThrowObject()
    {
        if (_currentObject == null) return;

        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.value);
        Vector2 playerPos = controller.transform.position;
        Vector2 dir = (mousePos - playerPos).normalized;

        _currentObject.Throw(dir);
        _currentObject = null; // Clear ref
    }

    // --- Swim Mechanics (Mirrors PlayerSwimState) ---

    private void HandleMovement()
    {
        // Horizontal Movement
        float targetX = frameInput.Move.x * CurrentStats.swimSpeed;
        float accelX = (Mathf.Abs(targetX) > 0.01f) ? CurrentStats.swimAcceleration : CurrentStats.swimDeceleration;
        float newX = Mathf.MoveTowards(rb.linearVelocity.x, targetX, accelX * Time.fixedDeltaTime);

        // Vertical Movement (Buoyancy)
        float targetY = CurrentStats.swimRiseSpeed; 
        float accelY = CurrentStats.swimRiseAcceleration; // [CHANGED] Use dedicated rise acceleration

        // Dive Logic: Only if input is Down AND we have stamina
        if (frameInput.Move.y < -0.1f && _currentStamina > 0)
        {
            targetY = -CurrentStats.swimDiveSpeed;
            accelY = CurrentStats.swimAcceleration; // [CHANGED] Use standard acceleration when diving
        }
        
        float newY = Mathf.MoveTowards(rb.linearVelocity.y, targetY, accelY * Time.fixedDeltaTime);

        rb.linearVelocity = new Vector2(newX, newY);
    }

    private void HandleStamina()
    {
        // Drain if diving
        if (frameInput.Move.y < -0.1f)
        {
            _currentStamina -= Time.fixedDeltaTime;
        }
        // Recover if surfaced
        else if (IsSurfaced())
        {
            _currentStamina += CurrentStats.swimStaminaRecoveryRate * Time.fixedDeltaTime;
        }
        _currentStamina = Mathf.Clamp(_currentStamina, 0, CurrentStats.maxSwimStamina);
    }

    private void HandleStaminaVisuals()
    {
        if (_spriteRenderer == null) return;

        float staminaPct = _currentStamina / CurrentStats.maxSwimStamina;

        if (staminaPct <= CurrentStats.staminaFlashThreshold && staminaPct > 0)
        {
            // Flash Red
            float t = Mathf.PingPong(Time.time * CurrentStats.flashFrequency, 1f);
            _spriteRenderer.color = Color.Lerp(_defaultColor, CurrentStats.lowStaminaColor, t);
        }
        else if (_currentStamina <= 0)
        {
            // Solid Red
            _spriteRenderer.color = CurrentStats.lowStaminaColor;
        }
        else
        {
            _spriteRenderer.color = _defaultColor;
        }
    }

    private void PerformSwimJump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, CurrentStats.swimJumpPower);
    }

    private bool IsInWater()
    {
        return Physics2D.OverlapBox(col.bounds.center, col.bounds.size * 0.8f, 0, CurrentStats.waterLayer);
    }

    private bool IsSurfaced()
    {
        // Check if the top of the collider is out of the water
        Vector2 topPoint = new Vector2(col.bounds.center.x, col.bounds.max.y);
        return !Physics2D.OverlapPoint(topPoint, CurrentStats.waterLayer);
    }
}