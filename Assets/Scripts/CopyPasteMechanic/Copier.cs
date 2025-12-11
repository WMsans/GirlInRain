using UnityEngine;
using System.Collections.Generic;

public class Copier : StateMachineRunner
{
    [Header("Copier Settings")]
    public float maxEnergy = 100f;
    public float currentEnergy;
    public float maxPasteDistance = 10f;
    public LayerMask obstacleLayer; // Layers that block pasting (e.g., Ground/Walls)

    [Header("State References")]
    public State copyState;
    public State pasteState;

    // Internal Memory
    public GameObject MemorizedObject { get; private set; }
    public Collider2D MemorizedCollider {get; private set;}
    public float MemorizedCost { get; private set; }
    
    public InputSystem_Actions inputSystemActions { get;private set; }

    // Copy Tracking
    private List<CopiableObject> activeCopies = new List<CopiableObject>();

    // Interaction Logic Variables
    private float interactHoldDuration = 0f;
    private bool hasTriggeredHold = false;

    private void Awake()
    {
        currentEnergy = maxEnergy;
        inputSystemActions = new InputSystem_Actions();
    }

    private void OnEnable()
    {
        inputSystemActions.Player.Crouch.Enable();
        inputSystemActions.Player.Attack.Enable();
        inputSystemActions.Player.Interact.Enable();
    }

    private void OnDisable()
    {
        inputSystemActions.Player.Crouch.Disable();
        inputSystemActions.Player.Attack.Disable();
        inputSystemActions.Player.Interact.Disable();
    }

    public void Memorize(GameObject prefab, float cost)
    {
        MemorizedCollider = prefab.GetComponent<Collider2D>();
        MemorizedObject = prefab;
        MemorizedCost = cost;
        Debug.Log($"Memorized: {prefab.name} (Cost: {cost})");
    }

    public bool ConsumeEnergy(float amount)
    {
        if (currentEnergy >= amount)
        {
            currentEnergy -= amount;
            return true;
        }
        return false;
    }

    public void RefundEnergy(float amount)
    {
        currentEnergy = Mathf.Min(currentEnergy + amount, maxEnergy);
    }

    public void RegisterCopy(CopiableObject obj)
    {
        obj.isCopy = true;
        activeCopies.Add(obj);
    }

    public void UnregisterCopy(CopiableObject obj)
    {
        if (activeCopies.Contains(obj))
        {
            activeCopies.Remove(obj);
        }
    }

    // Called by States to handle deletion logic
    public void HandleInteractionLogic()
    {
        // Check if the 'Interact' action exists to prevent errors
        // Assumes you have added 'Interact' to your Player action map
        var interactAction = inputSystemActions.Player.Interact;
        
        if (interactAction.WasPressedThisFrame())
        {
            // If we haven't triggered the hold action yet, treat it as a click
            if (!hasTriggeredHold)
            {
                TryDeleteOneCopy();
            }
        }

        if (interactAction.IsPressed())
        {
            interactHoldDuration += Time.deltaTime;

            // Hold Logic (1 seconds)
            if (interactHoldDuration >= 1.0f && !hasTriggeredHold)
            {
                DeleteAllCopies();
                hasTriggeredHold = true; // Prevent re-triggering while holding
            }
        }
        else
        {
            // Reset
            interactHoldDuration = 0f;
            hasTriggeredHold = false;
        }
    }

    private void TryDeleteOneCopy()
    {
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(UnityEngine.InputSystem.Mouse.current.position.value);
        Collider2D hit = Physics2D.OverlapPoint(mousePos);

        if (hit != null)
        {
            CopiableObject obj = hit.GetComponent<CopiableObject>();
            // Only destroy if it is a registered copy
            if (obj != null && obj.isCopy)
            {
                RefundEnergy(obj.energyCost);
                UnregisterCopy(obj);
                Destroy(obj.gameObject);
                Debug.Log("Copy destroyed, energy refunded.");
            }
        }
    }

    private void DeleteAllCopies()
    {
        int count = 0;
        // Loop backwards to remove items safely
        for (int i = activeCopies.Count - 1; i >= 0; i--)
        {
            if (activeCopies[i] != null)
            {
                RefundEnergy(activeCopies[i].energyCost);
                Destroy(activeCopies[i].gameObject);
                count++;
            }
        }
        activeCopies.Clear();
        Debug.Log($"Cleared all {count} copies. Energy refunded.");
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, maxPasteDistance);
    }
}
