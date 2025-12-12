using UnityEngine;

public class RoomCameraLimitTrigger : MonoBehaviour
{
    // Public getter for the collider bounds
    public Collider2D RoomCollider { get; private set; }

    private void Awake()
    {
        RoomCollider = GetComponent<Collider2D>();
    }
}