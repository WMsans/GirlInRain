using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(WeightGiver))]
public class ObjectBuoyancy : MonoBehaviour
{
    [Tooltip("Layer mask for water detection")]
    public LayerMask waterLayer;
    
    private Rigidbody2D _rb;
    private WeightGiver _weightGiver;
    private Collider2D _col;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _weightGiver = GetComponent<WeightGiver>();
        _col = GetComponent<Collider2D>();
    }

    private void FixedUpdate()
    {
        if (IsInWater())
        {
            ApplyBuoyancy();
        }
    }

    private bool IsInWater()
    {
        // Simple overlap check
        return Physics2D.OverlapBox(_col.bounds.center, _col.bounds.size * 0.8f, 0, waterLayer);
    }

    private void ApplyBuoyancy()
    {
        WeightClass wClass = _weightGiver.CurrentWeightClass;
        float buoyancyForce = 0f;
        float waterDrag = 2f;

        switch (wClass)
        {
            case WeightClass.Negative:
                // Already floats in air, floats even harder in water
                buoyancyForce = 20f; 
                break;
            case WeightClass.Light:
                // High buoyancy, floats on surface
                buoyancyForce = 15f; 
                break;
            case WeightClass.Medium:
                // Neutral/Slight float (like wood)
                // Assuming Mass is ~1 and Gravity is ~9.8. Force needs to counteract gravity.
                buoyancyForce = _rb.mass * Mathf.Abs(Physics2D.gravity.y) * 1.1f;
                break;
            case WeightClass.Heavy:
                // Sinks, but slower than in air
                buoyancyForce = _rb.mass * Mathf.Abs(Physics2D.gravity.y) * 0.5f;
                break;
            case WeightClass.Massive:
                // Unaffected
                buoyancyForce = 0f;
                break;
        }

        if (wClass != WeightClass.Massive)
        {
            _rb.AddForce(Vector2.up * buoyancyForce);
            
            // Apply water drag
            _rb.linearVelocity = Vector2.Lerp(_rb.linearVelocity, Vector2.zero, waterDrag * Time.fixedDeltaTime);
        }
    }
}