using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class GrabbableObject : MonoBehaviour
{
    public float throwSpeed = 20f;
    
    private Rigidbody2D rb;
    private Collider2D col;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
    }

    public void Grab()
    {
        rb.simulated = false;
        col.enabled = false; // Disable collision with player
        transform.localRotation = Quaternion.identity;
    }

    public void Throw(Vector2 direction)
    {
        rb.simulated = true;
        col.enabled = true;
        transform.parent = null;
        
        // Apply throw velocity
        rb.linearVelocity = direction.normalized * throwSpeed;
    }
}