using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D))]
public class WeightGiver : MonoBehaviour
{
    [Header("Weight Settings")]
    [SerializeField] private WeightClass weightClass = WeightClass.Medium;
    [SerializeField] private bool autoConfigureRigidbody = true;

    private const float LightMass = 0.5f;
    private const float MediumMass = 1f;
    private const float HeavyMass = 50f;
    private const float MassiveMass = 1000f;

    private Rigidbody2D _rb;

    public WeightClass CurrentWeightClass => weightClass;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        if (autoConfigureRigidbody)
        {
            ConfigureRigidbody();
        }
    }

    private void OnValidate()
    {
        if (_rb == null) _rb = GetComponent<Rigidbody2D>();
        if (autoConfigureRigidbody && _rb != null)
        {
            ConfigureRigidbody();
        }
    }

    private void ConfigureRigidbody()
    {
        switch (weightClass)
        {
            case WeightClass.Negative:
                _rb.mass = LightMass;
                _rb.gravityScale = -0.5f; // Floats up in air
                break;
            case WeightClass.Light:
                _rb.mass = LightMass;
                _rb.gravityScale = 0.5f; // Falls slowly
                break;
            case WeightClass.Medium:
                _rb.mass = MediumMass;
                _rb.gravityScale = 1f;
                break;
            case WeightClass.Heavy:
                _rb.mass = HeavyMass;
                _rb.gravityScale = 2f; // Falls fast
                break;
            case WeightClass.Massive:
                _rb.bodyType = RigidbodyType2D.Kinematic; // Unmovable by physics
                _rb.mass = MassiveMass;
                break;
        }
    }

    /// <summary>
    /// Returns the mass of this object times gravity.
    /// </summary>
    public float GetEffectiveWeight()
    {
        // Absolute value ensures negative gravity (balloons) still registers pressure if forced down
        return Mathf.Abs(_rb.mass * Physics2D.gravity.y * _rb.gravityScale);
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
        if (_rb.bodyType == RigidbodyType2D.Kinematic && weightClass != WeightClass.Massive) return 0f;

        float totalWeight = GetEffectiveWeight();

        List<ContactPoint2D> contacts = new List<ContactPoint2D>();
        _rb.GetContacts(contacts);

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