using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D))]
public class WeightGiver : MonoBehaviour
{
    [Header("Weight Settings")]
    [SerializeField] private WeightClass weightClass = WeightClass.Medium;
    [SerializeField] private bool autoConfigureRigidbody = true;

    [Header("Configuration")]
    [SerializeField] private float lightMass = 0.5f;
    [SerializeField] private float mediumMass = 1f;
    [SerializeField] private float heavyMass = 50f;
    [SerializeField] private float massiveMass = 1000f;

    private Rigidbody2D rb;

    public WeightClass CurrentWeightClass => weightClass;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (autoConfigureRigidbody)
        {
            ConfigureRigidbody();
        }
    }

    private void OnValidate()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (autoConfigureRigidbody && rb != null)
        {
            ConfigureRigidbody();
        }
    }

    private void ConfigureRigidbody()
    {
        switch (weightClass)
        {
            case WeightClass.Negative:
                rb.mass = lightMass;
                rb.gravityScale = -0.5f; // Floats up in air
                break;
            case WeightClass.Light:
                rb.mass = lightMass;
                rb.gravityScale = 0.5f; // Falls slowly
                break;
            case WeightClass.Medium:
                rb.mass = mediumMass;
                rb.gravityScale = 1f;
                break;
            case WeightClass.Heavy:
                rb.mass = heavyMass;
                rb.gravityScale = 2f; // Falls fast
                break;
            case WeightClass.Massive:
                rb.bodyType = RigidbodyType2D.Kinematic; // Unmovable by physics
                rb.mass = massiveMass;
                break;
        }
    }

    /// <summary>
    /// Returns the mass of this object times gravity.
    /// </summary>
    public float GetEffectiveWeight()
    {
        // Absolute value ensures negative gravity (balloons) still registers pressure if forced down
        return Mathf.Abs(rb.mass * Physics2D.gravity.y * rb.gravityScale);
    }

    /// <summary>
    /// Recursively calculates weight of this object plus any WeightGivers on top of it.
    /// </summary>
    public float GetFullyStackedWeight(HashSet<GameObject> visited = null)
    {
        if (visited == null) visited = new HashSet<GameObject>();
        if (visited.Contains(gameObject)) return 0f;
        visited.Add(gameObject);

        // If this object is held by the player, it shouldn't weigh down the plate directly
        // (The player's weight plus the held object should count, but that depends on Player implementation)
        if (rb.bodyType == RigidbodyType2D.Kinematic && weightClass != WeightClass.Massive) return 0f;

        float totalWeight = GetEffectiveWeight();

        List<ContactPoint2D> contacts = new List<ContactPoint2D>();
        rb.GetContacts(contacts);

        HashSet<GameObject> processedNeighbors = new HashSet<GameObject>();

        foreach (var contact in contacts)
        {
            if (contact.collider.transform.position.y > transform.position.y)
            {
                WeightGiver neighbor = contact.collider.GetComponent<WeightGiver>();
                if (neighbor != null && processedNeighbors.Add(neighbor.gameObject))
                {
                    totalWeight += neighbor.GetFullyStackedWeight(visited);
                }
            }
        }

        return totalWeight;
    }
}