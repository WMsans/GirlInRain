using System.Collections.Generic;
using UnityEngine;
using LDtkUnity; // Assuming standard namespace, adjust if your generated code differs

public class RoomLoadManager : MonoBehaviour
{
    public static RoomLoadManager Instance { get; private set; }

    // Dictionary to look up Level Components by their IID string
    private Dictionary<string, LDtkComponentLevel> _levelRegistry = new Dictionary<string, LDtkComponentLevel>();

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
                _levelRegistry.TryAdd(iidComponent.Iid, level);
            }
            else
            {
                Debug.LogWarning($"Level {level.gameObject.name} is missing LDtkIid component!");
            }
        }
    }

    public void OnEnterRoom(LDtkComponentLevel currentLevel)
    {
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