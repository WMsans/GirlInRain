using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem; // Added for Keyboard access

public class Copier : StateMachineRunner
{
    [Header("Copier Settings")]
    public float maxEnergy = 100f;
    public float currentEnergy;
    public float maxPasteDistance = 10f;
    public LayerMask obstacleLayer; // Layers that block pasting (e.g., Ground/Walls)

    [Header("Grid Settings")] // [New] Grid Settings
    public bool useGridSnap = false;
    public float gridSize = 1f;

    [Header("Visuals")]
    public GameObject pastePreviewPrefab;
    private GameObject _previewInstance;

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
        MemorizedCollider = prefab.GetComponentInChildren<Collider2D>();
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
    }

    public void UpdatePreview()
    {
        // 1. If we have nothing memorized, hide the preview
        if (MemorizedObject == null)
        {
            HidePreview();
            return;
        }

        // 2. Ensure preview instance exists
        if (!_previewInstance && pastePreviewPrefab != null)
        {
            _previewInstance = Instantiate(pastePreviewPrefab);
        }

        if (!_previewInstance) return;

        _previewInstance.SetActive(true);

        // 3. Update Position
        _previewInstance.transform.position = GetPastePosition();

        // 4. Update Visuals (Sprite) from MemorizedObject
        SpriteRenderer sourceSr = MemorizedObject.GetComponentInChildren<SpriteRenderer>();
        SpriteRenderer previewSr = _previewInstance.GetComponentInChildren<SpriteRenderer>();

        if (sourceSr != null && previewSr != null)
        {
            previewSr.sprite = sourceSr.sprite;
            // Optional: Make it semi-transparent
            Color c = sourceSr.color;
            c.a = 0.5f; 
            previewSr.color = c;
            
            // Match scale/rotation if needed
            previewSr.transform.localScale = sourceSr.transform.localScale;
            previewSr.transform.rotation = sourceSr.transform.rotation;
        }
    }

    public void HidePreview()
    {
        if (_previewInstance != null)
        {
            _previewInstance.SetActive(false);
        }
    }

    public Vector2 GetPastePosition()
    {
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.value);
        Vector2 playerPos = transform.position;
        Vector2 direction = (mousePos - playerPos);
        float distanceToMouse = direction.magnitude;
        Vector2 dirNormalized = direction.normalized;

        // 1. Clamp distance to max range
        float targetDistance = Mathf.Min(distanceToMouse, maxPasteDistance);

        // 2. Check for obstacles (Raycast)
        RaycastHit2D hit = Physics2D.Raycast(playerPos, dirNormalized, targetDistance, obstacleLayer);
        
        Vector2 finalPos;

        if (hit.collider != null)
        {
            // Offset position based on object extents to prevent wall clipping
            Vector2 extents = (MemorizedCollider != null) ? MemorizedCollider.bounds.extents : Vector2.zero;
            float projection = (Mathf.Abs(dirNormalized.x) * extents.x) + (Mathf.Abs(dirNormalized.y) * extents.y);

            finalPos = hit.point - (dirNormalized * projection);
        }
        else
        {
            finalPos = playerPos + (dirNormalized * targetDistance); 
        }

        if (useGridSnap)
        {
            finalPos.x = Mathf.Round(finalPos.x / gridSize) * gridSize;
            finalPos.y = Mathf.Round(finalPos.y / gridSize) * gridSize;
        }

        return finalPos;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, maxPasteDistance);

        if (useGridSnap)
        {
            Gizmos.color = new Color(1, 1, 1, 0.3f);
            Vector3 center = transform.position;
            // Draw a small 5x5 grid around player for visualization
            for (float x = -5; x <= 5; x += gridSize)
            {
                float snapX = Mathf.Round((center.x + x) / gridSize) * gridSize;
                Gizmos.DrawLine(new Vector3(snapX, center.y - 5, 0), new Vector3(snapX, center.y + 5, 0));
            }
            for (float y = -5; y <= 5; y += gridSize)
            {
                float snapY = Mathf.Round((center.y + y) / gridSize) * gridSize;
                Gizmos.DrawLine(new Vector3(center.x - 5, snapY, 0), new Vector3(center.x + 5, snapY, 0));
            }
        }
    }
}