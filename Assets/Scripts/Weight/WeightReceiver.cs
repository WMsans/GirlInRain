using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class WeightReceiver : MonoBehaviour
{
    [Header("Events")]
    public WeightEvent OnWeightReceived;
    public UnityEvent OnWeightRemoved;

    [Header("Debug")]
    [SerializeField] private float currentTotalWeight = 0f;

    // Track objects currently touching the receiver
    private HashSet<WeightGiver> contactingGivers = new HashSet<WeightGiver>();

    private void OnCollisionEnter2D(Collision2D collision)
    {
        WeightGiver giver = collision.gameObject.GetComponent<WeightGiver>();
        if (giver != null)
        {
            contactingGivers.Add(giver);
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        WeightGiver giver = collision.gameObject.GetComponent<WeightGiver>();
        if (giver != null)
        {
            contactingGivers.Remove(giver);
        }
    }

    private void FixedUpdate()
    {
        CalculateAndReportWeight();
    }

    private void CalculateAndReportWeight()
    {
        float newTotalWeight = 0f;
        
        // We use a visited set to handle pyramid stacks correctly 
        // (so a single top block isn't counted twice by two bottom blocks)
        HashSet<GameObject> visited = new HashSet<GameObject>();

        foreach (var giver in contactingGivers)
        {
            if (giver != null && giver.gameObject.activeInHierarchy)
            {
                newTotalWeight += giver.GetFullyStackedWeight(visited);
            }
        }

        // Only invoke events if the weight has changed significantly
        if (Mathf.Abs(newTotalWeight - currentTotalWeight) > 0.01f)
        {
            currentTotalWeight = newTotalWeight;

            if (currentTotalWeight > 0.01f)
            {
                // Force direction is generally assumed to be down (gravity) for a scale
                OnWeightReceived.Invoke(currentTotalWeight, Vector2.down);
            }
            else
            {
                // Clamp to 0 and fire remove event
                currentTotalWeight = 0f;
                OnWeightRemoved.Invoke();
            }
        }
    }
}

[System.Serializable]
public class WeightEvent : UnityEvent<float, Vector2> { }