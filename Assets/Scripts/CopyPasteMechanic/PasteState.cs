using UnityEngine;
using UnityEngine.InputSystem;

public class PasteState : State
{
    public override void OnUpdate(StateMachineRunner runner)
    {
        Copier copier = runner as Copier;
        if (copier == null) return;

        // Switch to Copy State
        if (copier.inputSystemActions.Player.Crouch.WasPressedThisFrame())
        {
            runner.ChangeState(copier.copyState);
            return;
        }

        // Paste Logic
        if (copier.inputSystemActions.Player.Attack.WasPressedThisFrame())
        {
            if (copier.MemorizedObject == null)
            {
                Debug.Log("No object memorized.");
                return;
            }

            // Check Energy
            if (copier.currentEnergy < copier.MemorizedCost)
            {
                Debug.Log("Not enough energy.");
                return;
            }

            // Calculate Position
            Vector2 spawnPos = CalculatePastePosition(copier);

            // Instantiate and Consume Energy
            Instantiate(copier.MemorizedObject, spawnPos, Quaternion.identity);
            copier.ConsumeEnergy(copier.MemorizedCost);
        }
    }

    private Vector2 CalculatePastePosition(Copier copier)
    {
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.value);
        Vector2 playerPos = copier.transform.position;
        Vector2 direction = (mousePos - playerPos);
        float distanceToMouse = direction.magnitude;

        // 1. Clamp distance to max range
        float targetDistance = Mathf.Min(distanceToMouse, copier.maxPasteDistance);
        Vector2 targetPos = playerPos + (direction.normalized * targetDistance);

        // 2. Check for obstacles (Line of Sight)
        // Raycast from player to target position to see if we hit a wall
        // We use the memorized collider's size.
        
        RaycastHit2D hit = Physics2D.BoxCast(playerPos, copier.MemorizedCollider.bounds.size, 0, direction.normalized, targetDistance, copier.obstacleLayer);
        
        if (hit.collider != null)
        {
            // FIX: Use hit.centroid (the center of the box at impact) instead of hit.point (the surface contact point).
            // This ensures the object is placed "touching" the wall but not inside it.
            return hit.centroid;
            
            // Alternative calculation if hit.centroid is not available in your Unity version:
            // return playerPos + (direction.normalized * hit.distance);
        }

        return targetPos; // Paste at mouse or max range
    }
}