using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(Camera))]
public class RoomCameraController : MonoBehaviour
{
    public static RoomCameraController Instance;

    [Header("Targeting")]
    [SerializeField] private Transform player;
    
    [Header("Settings")]
    [Tooltip("Time it takes to tween to the new room bounds")]
    [SerializeField] private float transitionDuration = 0.5f;
    [SerializeField] private Ease transitionEase = Ease.OutCubic;
    [Tooltip("How fast the camera follows the player within a room")]
    [SerializeField] private float followSpeed = 10f;

    private RoomCameraLimit _currentRoomCameraLimit;
    private Camera _cam;
    private float _camHeight;
    private float _camWidth;
    private bool _isTransitioning;

    private void Awake()
    {
        // Simple Singleton for easy access from Room scripts
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        _cam = GetComponent<Camera>();
        UpdateCameraSize();
    }

    private void Start()
    {
        // Optional: Initialize logic if player starts inside a room
        // You might want to manually find the starting room if it's not set
    }

    private void LateUpdate()
    {
        if (player == null || _currentRoomCameraLimit == null) return;

        // Calculate where the camera WANTS to be (centered on player)
        Vector3 targetPos = player.position;
        targetPos.z = transform.position.z; // Keep original Z depth

        // Clamp that target position to the current room's bounds
        Vector3 clampedPos = GetClampedPosition(targetPos, _currentRoomCameraLimit);

        if (_isTransitioning)
        {
            // If we are currently DOTweening (transitioning), 
            // we let DOTween handle the movement. Do nothing here.
            return;
        }

        // Standard smooth follow within the room
        transform.position = Vector3.Lerp(transform.position, clampedPos, followSpeed * Time.deltaTime);
    }

    public void TransitionToRoom(RoomCameraLimit newRoomCameraLimit)
    {
        if (_currentRoomCameraLimit == newRoomCameraLimit) return;

        _currentRoomCameraLimit = newRoomCameraLimit;
        _isTransitioning = true;

        // Recalculate camera size in case orthographic size changed
        UpdateCameraSize();

        // Determine where the camera should end up for the new room
        Vector3 targetPos = player.position;
        targetPos.z = transform.position.z;
        Vector3 finalPos = GetClampedPosition(targetPos, newRoomCameraLimit);

        // Kill any existing tweens on this transform to avoid conflicts
        transform.DOKill();

        // Use DOTween to smoothly move to the new correct position
        transform.DOMove(finalPos, transitionDuration)
            .SetEase(transitionEase)
            .OnUpdate(() => 
            {
                // Optional: If you want the target to update while tweening (if player keeps moving),
                // it gets complex. Usually, moving to the 'entry point' is sufficient.
            })
            .OnComplete(() => 
            {
                _isTransitioning = false;
            });
    }

    private Vector3 GetClampedPosition(Vector3 targetPosition, RoomCameraLimit roomCameraLimit)
    {
        Bounds bounds = roomCameraLimit.RoomCollider.bounds;

        // Calculate the min/max X and Y positions the camera can have
        // so it doesn't show anything outside the room
        float minX = bounds.min.x + _camWidth;
        float maxX = bounds.max.x - _camWidth;
        float minY = bounds.min.y + _camHeight;
        float maxY = bounds.max.y - _camHeight;

        // If the room is smaller than the camera view, center the camera on the room
        if (maxX < minX) 
        {
            targetPosition.x = bounds.center.x;
        }
        else
        {
            targetPosition.x = Mathf.Clamp(targetPosition.x, minX, maxX);
        }

        if (maxY < minY)
        {
            targetPosition.y = bounds.center.y;
        }
        else
        {
            targetPosition.y = Mathf.Clamp(targetPosition.y, minY, maxY);
        }

        return targetPosition;
    }

    private void UpdateCameraSize()
    {
        _camHeight = _cam.orthographicSize;
        _camWidth = _camHeight * _cam.aspect;
    }
}