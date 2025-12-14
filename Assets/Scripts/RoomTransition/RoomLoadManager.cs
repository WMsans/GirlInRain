using System.Collections.Generic;
using UnityEngine;
using LDtkUnity; 

public class RoomLoadManager : MonoBehaviour
{
    public static RoomLoadManager Instance { get; private set; }

    // Dictionary to look up Level Components by their IID string
    private Dictionary<string, LDtkComponentLevel> _levelRegistry = new Dictionary<string, LDtkComponentLevel>();
    // Cache level colliders for efficient bounds checking
    private Dictionary<string, Collider2D> _levelColliders = new Dictionary<string, Collider2D>();

    [Header("Detection Settings")]
    [SerializeField] private Transform player;
    
    private LDtkComponentLevel _currentLevel;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        // Initialize the registry
        RegisterAllLevels();
    }

    private void Start()
    {
        // Auto-find player if not assigned
        if (player == null && PlayerController.Instance != null)
        {
            player = PlayerController.Instance.transform;
        }
    }

    private void Update()
    {
        if (player == null) return;
        
        // Actively check which room the player is inside, mirroring CameraController logic
        CheckActiveRoom();
    }

    private void RegisterAllLevels()
    {
        // Find all levels currently in the scene (since they start active)
        LDtkComponentLevel[] allLevels = FindObjectsByType<LDtkComponentLevel>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (var level in allLevels)
        {
            // We need the LDtkIid component to get the unique ID
            var iidComponent = level.GetComponent<LDtkIid>();
            if (iidComponent != null)
            {
                if (_levelRegistry.TryAdd(iidComponent.Iid, level))
                {
                    // Cache the collider for this level
                    var col = level.GetComponent<Collider2D>();
                    if (col != null)
                    {
                        _levelColliders[iidComponent.Iid] = col;
                    }
                }
            }
            else
            {
                Debug.LogWarning($"Level {level.gameObject.name} is missing LDtkIid component!");
            }
        }
    }

    private void CheckActiveRoom()
    {
        LDtkComponentLevel bestLevel = null;
        float minDistanceSqr = float.MaxValue;

        // Iterate through all registered levels to find the best fit
        foreach (var kvp in _levelRegistry)
        {
            string iid = kvp.Key;
            LDtkComponentLevel level = kvp.Value;

            // Skip if level or its collider is invalid
            // Note: We check activeInHierarchy because generally we only want to transition 
            // to levels that are already loaded (neighbors).
            if (level == null || !level.gameObject.activeInHierarchy) 
                continue;

            if (_levelColliders.TryGetValue(iid, out Collider2D col) && col != null && col.enabled)
            {
                // Check if player is inside the bounds
                if (col.bounds.Contains(player.position))
                {
                    // Pick the room where the player is closest to the center
                    float distSqr = (player.position - col.bounds.center).sqrMagnitude;
                    if (distSqr < minDistanceSqr)
                    {
                        minDistanceSqr = distSqr;
                        bestLevel = level;
                    }
                }
            }
        }

        // If we found a valid room and it's different from the current one, switch.
        if (bestLevel != null && bestLevel != _currentLevel)
        {
            OnEnterRoom(bestLevel);
        }
    }

    public void OnEnterRoom(LDtkComponentLevel currentLevel)
    {
        if (_currentLevel != currentLevel)
            _currentLevel = currentLevel;

        // 1. Identify which rooms should be active (Current + Neighbors)
        HashSet<string> activeIids = new HashSet<string>();

        // Add current room IID
        string currentIid = currentLevel.GetComponent<LDtkIid>().Iid;
        activeIids.Add(currentIid);

        // Add neighbor IIDs
        foreach (LDtkNeighbour neighbour in currentLevel.Neighbours)
        {
            // Use the method provided in your description
            LDtkIid neighborIidComponent = neighbour.FindLevel();
            
            if (neighborIidComponent != null)
            {
                activeIids.Add(neighborIidComponent.Iid);
            }
        }

        // 2. Iterate through all registered levels and Toggle them
        foreach (var kvp in _levelRegistry)
        {
            string iid = kvp.Key;
            LDtkComponentLevel levelObj = kvp.Value;

            bool shouldBeActive = activeIids.Contains(iid);

            // Only toggle if the state is actually changing to avoid overhead
            if (levelObj.gameObject.activeSelf != shouldBeActive)
            {
                levelObj.gameObject.SetActive(shouldBeActive);
            }
        }
    }
}