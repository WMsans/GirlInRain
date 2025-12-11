using UnityEngine;

public class RoomCameraLimitTrigger : MonoBehaviour
{
    // Public getter for the collider bounds
    public Collider2D RoomCollider { get; private set; }

    private void Awake()
    {
        RoomCollider = GetComponent<Collider2D>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // When the player enters this trigger, tell the camera to switch to this room
        if (collision.CompareTag("Player"))
        {
            RoomCameraController.Instance.TransitionToRoom(this);
        }
    }
}