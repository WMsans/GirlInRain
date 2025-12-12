using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class CopiableObject : MonoBehaviour
{
    [Tooltip("The energy required to paste this object.")]
    public float energyCost = 10f;

    [Tooltip("The prefab to spawn when pasted. If null, copies this gameObject.")]
    public GameObject prefabToSpawn;

    public UnityEvent onHover;
    public UnityEvent onHoverEnd;

    // Flag to distinguish original objects from spawned copies
    [HideInInspector]
    public bool isCopy = false;

    private Copier _copier;
    public Collider2D col;
    private bool _isHoveredValid;

    private void Start()
    {
        _copier = FindFirstObjectByType<Copier>();
        if(!col) col = GetComponent<Collider2D>();
    }

    private void Update()
    {
        if (_copier == null || col == null || Mouse.current == null) return;

        // Perform manual overlap check to match Copier's input logic
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.value);
        bool isOver = col.OverlapPoint(mousePos);

        if (isOver)
        {
            // Determine if interaction is possible
            bool canCopy = _copier.CurrentState is CopyState;
            // Removal is allowed in both Copy and Paste states, but only for copies
            bool canRemove = /*isCopy*/ false; 

            if (canCopy || canRemove)
            {
                if (!_isHoveredValid)
                {
                    _isHoveredValid = true;
                    onHover?.Invoke();
                }
                return; // Interaction is valid, skip the disable block
            }
        }

        // If we reach here, we are either not hovering, or the state is invalid for interaction
        if (_isHoveredValid)
        {
            _isHoveredValid = false;
            onHoverEnd?.Invoke();
        }
    }

    // Helper to get the correct object to memorize
    public GameObject GetSourcePrefab()
    {
        return prefabToSpawn != null ? prefabToSpawn : this.gameObject;
    }
}