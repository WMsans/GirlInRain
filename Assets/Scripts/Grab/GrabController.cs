using UnityEngine;
using UnityEngine.InputSystem;

public class GrabController : MonoBehaviour
{
    [Header("References")]
    public PlayerController player;
    public PlayerGrabState grabState;
    public PlayerSwimGrabState swimGrabState; // Added reference
    public Transform holdPoint;

    [Header("Settings")]
    public float grabRadius = 1.5f;
    public LayerMask grabLayer;

    private void Update()
    {
        // Allow grabbing from Normal, Swim, or other neutral states
        if (player.stateMachine.CurrentState is PlayerNormalState || 
            player.stateMachine.CurrentState is PlayerSwimState)
        {
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

        RaycastHit2D hit = Physics2D.Raycast(playerPos, dir, grabRadius, grabLayer);

        if (hit.collider != null)
        {
            GrabbableObject grabbable = hit.collider.GetComponent<GrabbableObject>();
            
            if (grabbable != null && grabbable.CanBeGrabbed())
            {
                // Logic Check: Are we in water?
                bool inWater = IsPlayerInWater();

                if (inWater && swimGrabState != null)
                {
                    // Switch to Swim Grab
                    swimGrabState.Initialize(grabbable, holdPoint);
                    player.stateMachine.ChangeState(swimGrabState);
                }
                else if (grabState != null)
                {
                    // Switch to Standard Grab
                    grabState.Initialize(grabbable, holdPoint);
                    player.stateMachine.ChangeState(grabState);
                }
            }
        }
    }

    private bool IsPlayerInWater()
    {
        // Reusing the overlap check logic found in other states
        // Ideally this should be a public property on PlayerController, 
        // but checking here is safe for now.
        if (player.stats == null) return false;
        return Physics2D.OverlapBox(player.col.bounds.center, player.col.bounds.size * 0.8f, 0, player.stats.waterLayer);
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