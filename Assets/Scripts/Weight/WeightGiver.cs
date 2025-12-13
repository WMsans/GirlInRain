using UnityEngine;
using System.Collections.Generic;

public class WeightGiver : MonoBehaviour
{
    [SerializeField] private Rigidbody2D rb;

    private void Awake()
    {
        if (!rb) rb = GetComponent<Rigidbody2D>();
    }

    /// <summary>
    /// Returns the mass of this object times gravity.
    /// </summary>
    public float GetEffectiveWeight()
    {
        return Mathf.Abs(rb.mass * Physics2D.gravity.y * rb.gravityScale);
    }

    /// <summary>
    /// Recursively calculates weight of this object plus any WeightGivers on top of it.
    /// </summary>
    public float GetFullyStackedWeight(HashSet<GameObject> visited = null)
    {
        // Prevent infinite recursion loops (e.g. A on B, B on A)
        if (visited == null) visited = new HashSet<GameObject>();
        if (visited.Contains(gameObject)) return 0f;
        visited.Add(gameObject);

        float totalWeight = GetEffectiveWeight();

        // Check for contacts
        List<ContactPoint2D> contacts = new List<ContactPoint2D>();
        rb.GetContacts(contacts);

        HashSet<GameObject> processedNeighbors = new HashSet<GameObject>();

        foreach (var contact in contacts)
        {
            // We want objects that are physically "above" this one.
            // A simple robust check is comparing Y positions.
            // If the other object is higher, we consider it part of the stack on top.
            if (contact.collider.transform.position.y > transform.position.y)
            {
                WeightGiver neighbor = contact.collider.GetComponent<WeightGiver>();
                
                // Ensure we have a valid neighbor, haven't processed it this frame, and haven't visited it yet
                if (neighbor != null && processedNeighbors.Add(neighbor.gameObject))
                {
                    totalWeight += neighbor.GetFullyStackedWeight(visited);
                }
            }
        }

        return totalWeight;
    }
}