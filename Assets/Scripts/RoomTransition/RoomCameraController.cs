using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;

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

    private RoomCameraLimitTrigger _currentRoomCameraLimitTrigger;
    
    // Store all potential rooms in the scene
    private List<RoomCameraLimitTrigger> _allRoomTriggers = new List<RoomCameraLimitTrigger>();
    
    private Camera _cam;
    private float _camHeight;
    private float _camWidth;
    private bool _isTransitioning;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        _cam = GetComponent<Camera>();
        UpdateCameraSize();
    }

    private void Start()
    {
        // Find ALL room triggers in the scene, even those currently inactive.
        // We do this in Start to ensure their Awake() methods have run and cached their colliders.
        // Note: Check for both active and inactive objects to ensure we have the full list.
        var triggers = FindObjectsByType<RoomCameraLimitTrigger>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        _allRoomTriggers.AddRange(triggers);
    }

    private void LateUpdate()
    {
        if (player == null) return;

        // 1. Actively check which room the player is inside
        CheckActiveRoom();

        if (_currentRoomCameraLimitTrigger == null) return;

        // 2. Calculate target position
        Vector3 targetPos = player.position;
        targetPos.z = transform.position.z;

        Vector3 clampedPos = GetClampedPosition(targetPos, _currentRoomCameraLimitTrigger);

        if (_isTransitioning) return;

        // 3. Smooth follow
        transform.position = Vector3.Lerp(transform.position, clampedPos, followSpeed * Time.deltaTime);
    }

    private void CheckActiveRoom()
    {
        RoomCameraLimitTrigger bestRoom = null;
        float minDistanceSqr = float.MaxValue;

        // Iterate through all known rooms to find the best fit
        foreach (var room in _allRoomTriggers)
        {
            // Skip if the room object or its collider is disabled (e.g. by RoomLoadManager)
            if (room == null || !room.gameObject.activeInHierarchy || !room.RoomCollider.enabled) 
                continue;

            // Check if player is strictly inside the bounds
            if (room.RoomCollider.bounds.Contains(player.position))
            {
                // If the player is in multiple overlapping rooms, pick the one 
                // where the player is closest to the center.
                float distSqr = (player.position - room.RoomCollider.bounds.center).sqrMagnitude;
                if (distSqr < minDistanceSqr)
                {
                    minDistanceSqr = distSqr;
                    bestRoom = room;
                }
            }
        }

        // If we found a valid room and it's different from the current one, switch.
        if (bestRoom != null && bestRoom != _currentRoomCameraLimitTrigger)
        {
            TransitionToRoom(bestRoom);
        }
    }

    public void TransitionToRoom(RoomCameraLimitTrigger newRoomCameraLimitTrigger)
    {
        if (_currentRoomCameraLimitTrigger == newRoomCameraLimitTrigger) return;

        _currentRoomCameraLimitTrigger = newRoomCameraLimitTrigger;
        _isTransitioning = true;

        UpdateCameraSize();

        Vector3 targetPos = player.position;
        targetPos.z = transform.position.z;
        Vector3 finalPos = GetClampedPosition(targetPos, newRoomCameraLimitTrigger);

        transform.DOKill();
        transform.DOMove(finalPos, transitionDuration)
            .SetEase(transitionEase)
            .OnComplete(() => 
            {
                _isTransitioning = false;
            });
    }

    private Vector3 GetClampedPosition(Vector3 targetPosition, RoomCameraLimitTrigger roomCameraLimitTrigger)
    {
        Bounds bounds = roomCameraLimitTrigger.RoomCollider.bounds;

        float minX = bounds.min.x + _camWidth;
        float maxX = bounds.max.x - _camWidth;
        float minY = bounds.min.y + _camHeight;
        float maxY = bounds.max.y - _camHeight;

        if (maxX < minX) targetPosition.x = bounds.center.x;
        else targetPosition.x = Mathf.Clamp(targetPosition.x, minX, maxX);

        if (maxY < minY) targetPosition.y = bounds.center.y;
        else targetPosition.y = Mathf.Clamp(targetPosition.y, minY, maxY);

        return targetPosition;
    }

    private void UpdateCameraSize()
    {
        _camHeight = _cam.orthographicSize;
        _camWidth = _camHeight * _cam.aspect;
    }
}