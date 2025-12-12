using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class Copier : StateMachineRunner
{
    [Header("Copier Settings")]
    public float maxEnergy = 100f;
    public float currentEnergy;
    public float maxPasteDistance = 10f;
    public LayerMask obstacleLayer; // Layers that block pasting (e.g., Ground/Walls)

    [Header("Paste Logic")]
    [Tooltip("If true, uses a Raycast from player to target to prevent pasting through walls (Path Overlap). If false, directly tests the target position (Target Overlap).")]
    public bool checkPathCollisions = true;

    [Tooltip("Only used if 'Check Path Collisions' is false. If the target position is blocked, should we try to find the nearest empty slot?")]
    public bool attemptAutoSnap = false;

    [Tooltip("If Auto Snap is enabled, how far should we search for an empty slot?")]
    public float autoSnapSearchRadius = 3f;

    [Header("Grid Settings")]
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
    public Collider2D MemorizedCollider { get; private set; }
    public float MemorizedCost { get; private set; }
    
    public InputSystem_Actions inputSystemActions { get; private set; }

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

    public void HandleInteractionLogic()
    {
        var interactAction = inputSystemActions.Player.Interact;
        
        if (interactAction.WasPressedThisFrame())
        {
            if (!hasTriggeredHold)
            {
                TryDeleteOneCopy();
            }
        }

        if (interactAction.IsPressed())
        {
            interactHoldDuration += Time.deltaTime;

            if (interactHoldDuration >= 1.0f && !hasTriggeredHold)
            {
                DeleteAllCopies();
                hasTriggeredHold = true;
            }
        }
        else
        {
            interactHoldDuration = 0f;
            hasTriggeredHold = false;
        }
    }

    private void TryDeleteOneCopy()
    {
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.value);
        Collider2D hit = Physics2D.OverlapPoint(mousePos);

        if (hit != null)
        {
            CopiableObject obj = hit.GetComponent<CopiableObject>();
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
        for (int i = activeCopies.Count - 1; i >= 0; i--)
        {
            if (activeCopies[i] != null)
            {
                RefundEnergy(activeCopies[i].energyCost);
                Destroy(activeCopies[i].gameObject);
            }
        }
        activeCopies.Clear();
    }

    public void UpdatePreview()
    {
        if (MemorizedObject == null)
        {
            HidePreview();
            return;
        }

        if (!_previewInstance && pastePreviewPrefab != null)
        {
            _previewInstance = Instantiate(pastePreviewPrefab);
        }

        if (!_previewInstance) return;

        // Get Position and Validity
        Vector2 pastePos = GetPastePosition(out bool isValid);

        _previewInstance.SetActive(true);
        _previewInstance.transform.position = pastePos;

        SpriteRenderer sourceSr = MemorizedObject.GetComponentInChildren<SpriteRenderer>();
        SpriteRenderer previewSr = _previewInstance.GetComponentInChildren<SpriteRenderer>();

        if (sourceSr != null && previewSr != null)
        {
            previewSr.sprite = sourceSr.sprite;
            
            // Visual feedback for Invalid placement
            Color c = sourceSr.color;
            if (isValid)
            {
                c.a = 0.5f; // Semi-transparent valid
                previewSr.color = c;
            }
            else
            {
                previewSr.color = new Color(1f, 0f, 0f, 0.5f); // Red invalid
            }
            
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

    /// <summary>
    /// Calculates the paste position based on current settings.
    /// Returns true via 'isValid' if the position is safe to paste, false if blocked/prevented.
    /// </summary>
    public Vector2 GetPastePosition(out bool isValid)
    {
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.value);
        Vector2 playerPos = transform.position;
        Vector2 direction = (mousePos - playerPos);
        float distanceToMouse = direction.magnitude;
        Vector2 dirNormalized = direction.normalized;

        isValid = true;
        Vector2 finalPos = Vector2.zero;

        // --- OPTION A: RAYCAST PATH (Original Behavior) ---
        if (checkPathCollisions)
        {
            float targetDistance = Mathf.Min(distanceToMouse, maxPasteDistance);
            RaycastHit2D hit = Physics2D.Raycast(playerPos, dirNormalized, targetDistance, obstacleLayer);

            if (hit.collider != null)
            {
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
                finalPos = SnapToGrid(finalPos);
            }
            
            return finalPos;
        }

        // --- OPTION B: DIRECT TEST (New Behavior) ---
        
        // 1. Determine ideal target based on mouse cursor (clamped to max distance)
        float clampedDistance = Mathf.Min(distanceToMouse, maxPasteDistance);
        Vector2 targetPos = playerPos + (dirNormalized * clampedDistance);

        if (useGridSnap)
        {
            targetPos = SnapToGrid(targetPos);
        }

        // 2. Check overlap at target
        if (IsPositionValid(targetPos))
        {
            return targetPos;
        }
        else
        {
            // Position is blocked.
            if (attemptAutoSnap)
            {
                // 3. Try to find nearest empty slot
                if (TryFindNearestEmptySlot(targetPos, playerPos, out Vector2 foundPos))
                {
                    return foundPos;
                }
            }
            
            // If we are here, we are blocked and either autoSnap is off, or it failed.
            isValid = false;
            return targetPos;
        }
    }

    private Vector2 SnapToGrid(Vector2 pos)
    {
        pos.x = Mathf.Round(pos.x / gridSize) * gridSize;
        pos.y = Mathf.Round(pos.y / gridSize) * gridSize;
        return pos;
    }

    private bool IsPositionValid(Vector2 pos)
    {
        if (MemorizedCollider == null) return true;

        // Use OverlapBox assuming the object is roughly box-shaped, or use the collider bounds size
        Vector2 size = MemorizedCollider.bounds.size;
        // Decrease size slightly to avoid floating point errors with adjacent tiles
        size *= 0.95f; 

        Collider2D hit = Physics2D.OverlapBox(pos, size, 0f, obstacleLayer);
        return hit == null;
    }

    private bool TryFindNearestEmptySlot(Vector2 origin, Vector2 playerPos, out Vector2 result)
    {
        // Spiral / Radial Search
        // To prefer player direction, we can sort candidates by distance to player? 
        // Or simply check "pull back" positions first? 
        // A robust way is to check points in a grid around 'origin' within 'autoSnapSearchRadius'.

        List<Vector2> candidates = new List<Vector2>();
        
        float step = useGridSnap ? gridSize : 0.5f; 
        int steps = Mathf.CeilToInt(autoSnapSearchRadius / step);

        for (int x = -steps; x <= steps; x++)
        {
            for (int y = -steps; y <= steps; y++)
            {
                if (x == 0 && y == 0) continue; // Skip center (already checked)

                Vector2 offset = new Vector2(x * step, y * step);
                if (offset.magnitude > autoSnapSearchRadius) continue;

                candidates.Add(origin + offset);
            }
        }

        // Sort candidates:
        // Primary Metric: Distance from original target (Closest to where user clicked)
        // Secondary Metric: Distance to Player (Prefer positions closer to player / along the path)
        candidates.Sort((a, b) =>
        {
            float distToTargetA = Vector2.SqrMagnitude(a - origin);
            float distToTargetB = Vector2.SqrMagnitude(b - origin);
            
            // Tolerance to group by "rings"
            if (Mathf.Abs(distToTargetA - distToTargetB) > 0.01f)
            {
                return distToTargetA.CompareTo(distToTargetB);
            }
            
            // If roughly same distance from target, pick the one closer to player
            float distToPlayerA = Vector2.SqrMagnitude(a - playerPos);
            float distToPlayerB = Vector2.SqrMagnitude(b - playerPos);
            return distToPlayerA.CompareTo(distToPlayerB);
        });

        foreach (Vector2 p in candidates)
        {
            if (IsPositionValid(p))
            {
                result = p;
                return true;
            }
        }

        result = origin;
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, maxPasteDistance);

        if (checkPathCollisions == false && attemptAutoSnap)
        {
             Gizmos.color = Color.cyan;
             // Draw search radius around mouse approx location for visualization
             Vector2 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.value);
             Gizmos.DrawWireSphere(mousePos, autoSnapSearchRadius);
        }
    }
}