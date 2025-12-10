using UnityEngine;

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

    private void Awake()
    {
        currentEnergy = maxEnergy;
        inputSystemActions = new InputSystem_Actions();
    }

    private void OnEnable()
    {
        inputSystemActions.Player.Crouch.Enable();
        inputSystemActions.Player.Attack.Enable();
    }

    private void OnDisable()
    {
        inputSystemActions.Player.Crouch.Disable();
        inputSystemActions.Player.Attack.Disable();
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

    // Helper to visualize paste range in editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, maxPasteDistance);
    }
}