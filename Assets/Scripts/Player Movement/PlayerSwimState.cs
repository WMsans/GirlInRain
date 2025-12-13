using UnityEngine;
using UnityEngine.Serialization;

public class PlayerSwimState : PlayerState
{
    [SerializeField] private PlayerStats customStats;
    private PlayerStats CurrentStats => customStats ? customStats : PlayerStats.Instance;

    private float _currentStamina;
    [SerializeField] private SpriteRenderer spriteRenderer;
    private Color _defaultColor;
    private bool _isFlashing;

    // Movement smoothing
    private Vector2 _currentVelocity;

    public override void OnEnter(StateMachineRunner runner)
    {
        base.OnEnter(runner);

        // Find sprite renderer for flashing effects
        if (!spriteRenderer) 
            spriteRenderer = controller.GetComponentInChildren<SpriteRenderer>();
        
        if (spriteRenderer) 
            _defaultColor = spriteRenderer.color;

        // Reset Physics
        rb.gravityScale = 0; // Disable gravity in water
        _currentStamina = CurrentStats.maxSwimStamina;
        
        // Dampen entry velocity
        rb.linearVelocity *= 0.25f;
    }

    public override void OnExit(StateMachineRunner runner)
    {
        // Restore Physics and Color
        rb.gravityScale = 1; // Default gravity (adjust if your game uses different scale)
        if (spriteRenderer) 
            spriteRenderer.color = _defaultColor;
        
        base.OnExit(runner);
    }

    public override void OnUpdate(StateMachineRunner runner)
    {
        base.OnUpdate(runner);
        HandleStaminaVisuals();

        if (frameInput.JumpDown)
        {
            PerformSwimJump(runner);
        }
    }

    public override void OnFixedUpdate(StateMachineRunner runner)
    {
        // Check if we are still in water
        if (!IsInWater())
        {
            runner.ChangeState(GetComponent<PlayerNormalState>() ?? FindFirstObjectByType<PlayerNormalState>());
            return;
        }

        HandleMovement();
        HandleStamina();
    }

    private void HandleMovement()
    {
        // Horizontal Movement
        float targetX = frameInput.Move.x * CurrentStats.swimSpeed;
        float accel = (Mathf.Abs(targetX) > 0.01f) ? CurrentStats.swimAcceleration : CurrentStats.swimDeceleration;
        
        float newX = Mathf.MoveTowards(rb.linearVelocity.x, targetX, accel * Time.fixedDeltaTime);

        // Vertical Movement (Buoyancy vs Diving)
        float targetY = CurrentStats.swimRiseSpeed; // Default: Float up

        // Dive Logic: Only if input is Down AND we have stamina
        if (frameInput.Move.y < -0.1f && _currentStamina > 0)
        {
            targetY = -CurrentStats.swimDiveSpeed;
        }
        
        float newY = Mathf.MoveTowards(rb.linearVelocity.y, targetY, CurrentStats.swimAcceleration * Time.fixedDeltaTime);

        rb.linearVelocity = new Vector2(newX, newY);
    }

    private void HandleStamina()
    {
        // If diving, reduce stamina
        if (frameInput.Move.y < -0.1f)
        {
            _currentStamina -= Time.fixedDeltaTime;
        }
        // Only recover stamina if not diving AND at the surface (top of player is out of water)
        else if (IsSurfaced())
        {
            _currentStamina += CurrentStats.swimStaminaRecoveryRate * Time.fixedDeltaTime;
        }

        _currentStamina = Mathf.Clamp(_currentStamina, 0, CurrentStats.maxSwimStamina);
    }

    private bool IsSurfaced()
    {
        // Check if the top point of the collider is NOT overlapping with water
        // This assumes IsInWater() (center check) is already true
        Vector2 topPoint = new Vector2(col.bounds.center.x, col.bounds.max.y);
        return !Physics2D.OverlapPoint(topPoint, CurrentStats.waterLayer);
    }

    private void HandleStaminaVisuals()
    {
        if (spriteRenderer == null) return;

        float staminaPct = _currentStamina / CurrentStats.maxSwimStamina;

        if (staminaPct <= CurrentStats.staminaFlashThreshold && staminaPct > 0)
        {
            // Flash Red
            float t = Mathf.PingPong(Time.time * CurrentStats.flashFrequency, 1f);
            spriteRenderer.color = Color.Lerp(_defaultColor, CurrentStats.lowStaminaColor, t);
        }
        else if (_currentStamina <= 0)
        {
            // Solid Red (or different visual) when completely empty
            spriteRenderer.color = CurrentStats.lowStaminaColor;
        }
        else
        {
            spriteRenderer.color = _defaultColor;
        }
    }

    private void PerformSwimJump(StateMachineRunner runner)
    {
        // Simple water jump/exit logic
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, CurrentStats.swimJumpPower);
        // We will likely exit the water collider next frame due to velocity
    }

    private bool IsInWater()
    {
        // Check for overlap with Water Layer
        return Physics2D.OverlapBox(col.bounds.center, col.bounds.size * 0.8f, 0, CurrentStats.waterLayer);
    }
}