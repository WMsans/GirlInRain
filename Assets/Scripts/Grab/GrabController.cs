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
        if (player.stateMachine.CurrentState is PlayerNormalState)
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
            // ADDED: Check if the object allows grabbing based on weight
            if (grabbable != null && grabbable.CanBeGrabbed())
            {
                grabState.Initialize(grabbable, holdPoint);
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