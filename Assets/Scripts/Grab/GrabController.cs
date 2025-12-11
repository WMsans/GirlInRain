using UnityEngine;
using UnityEngine.InputSystem;

public class GrabController : MonoBehaviour
{
    [Header("References")]
    public PlayerController player;
    public PlayerGrabState grabState;
    public Transform holdPoint;

    [Header("Settings")]
    public float grabRadius = 1.5f;
    public LayerMask grabLayer;

    private void Update()
    {
        // Only allow grabbing if we are currently in the Normal State
        if (player.stateMachine.CurrentState is PlayerNormalState)
        {
            // Use DashDown (mapped to Sprint) for grabbing
            if (GameInputManager.Instance.CurrentFrameInput.DashDown)
            {
                AttemptGrab();
            }
        }
    }

    private void AttemptGrab()
    {
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.value);
        Vector2 playerPos = player.transform.position;
        Vector2 dir = (mousePos - playerPos).normalized;

        // Check for objects in the direction of the mouse
        RaycastHit2D hit = Physics2D.Raycast(playerPos, dir, grabRadius, grabLayer);

        if (hit.collider != null)
        {
            GrabbableObject grabbable = hit.collider.GetComponent<GrabbableObject>();
            if (grabbable != null)
            {
                // Initialize the state with the object and hold point
                grabState.Initialize(grabbable, holdPoint);
                
                // Force state change
                player.stateMachine.ChangeState(grabState);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (player != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(player.transform.position, grabRadius);
        }
    }
}