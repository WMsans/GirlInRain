using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class GrabbableObject : MonoBehaviour
{
    public float throwSpeed = 20f;
    
    private Rigidbody2D rb;
    private Collider2D col;
    private WeightGiver weightGiver;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        weightGiver = GetComponent<WeightGiver>();
    }

    public bool CanBeGrabbed()
    {
        if (weightGiver == null) return true; // Default to true if no weight script

        switch (weightGiver.CurrentWeightClass)
        {
            case WeightClass.Negative:
            case WeightClass.Light:
            case WeightClass.Medium:
                return true;
            case WeightClass.Heavy:
            case WeightClass.Massive:
                return false;
            default:
                return true;
        }
    }

    public void Grab()
    {
        if (!CanBeGrabbed()) return;

        rb.simulated = false;
        col.enabled = false; 
        transform.localRotation = Quaternion.identity;
    }

    public void Throw(Vector2 direction)
    {
        rb.simulated = true;
        col.enabled = true;
        transform.parent = null;
        
        // Adjust throw speed based on weight?
        float speedModifier = 1f;
        if (weightGiver != null && weightGiver.CurrentWeightClass == WeightClass.Light) speedModifier = 1.5f;

        rb.linearVelocity = direction.normalized * (throwSpeed * speedModifier);
    }
}