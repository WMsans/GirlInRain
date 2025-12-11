using UnityEngine;

public class CopiableObject : MonoBehaviour
{
    [Tooltip("The energy required to paste this object.")]
    public float energyCost = 10f;

    [Tooltip("The prefab to spawn when pasted. If null, copies this gameObject.")]
    public GameObject prefabToSpawn;

    // Flag to distinguish original objects from spawned copies
    [HideInInspector]
    public bool isCopy = false;

    // Helper to get the correct object to memorize
    public GameObject GetSourcePrefab()
    {
        return prefabToSpawn != null ? prefabToSpawn : this.gameObject;
    }
}
