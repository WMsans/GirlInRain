using UnityEngine;
using UnityEngine.InputSystem;

public class PasteState : State
{
    public override void OnUpdate(StateMachineRunner runner)
    {
        Copier copier = runner as Copier;
        if (copier == null) return;

        // Interaction Logic (Destroy / Clear All)
        copier.HandleInteractionLogic();

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

            // Instantiate, Register, and Consume Energy
            GameObject newObj = Instantiate(copier.MemorizedObject, spawnPos, Quaternion.identity);
            
            // Register as copy so it can be destroyed later
            CopiableObject copiable = newObj.GetComponent<CopiableObject>();
            if (copiable != null)
            {
                copier.RegisterCopy(copiable);
            }

            copier.ConsumeEnergy(copier.MemorizedCost);
        }
    }

    private Vector2 CalculatePastePosition(Copier copier)
    {
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.value);
        Vector2 playerPos = copier.transform.position;
        Vector2 direction = (mousePos - playerPos);
        float distanceToMouse = direction.magnitude;
        Vector2 dirNormalized = direction.normalized;

        // 1. Clamp distance to max range
        float targetDistance = Mathf.Min(distanceToMouse, copier.maxPasteDistance);

        // 2. Check for obstacles (Raycast)
        // [Modified] Used Raycast instead of BoxCast. 
        RaycastHit2D hit = Physics2D.Raycast(playerPos, dirNormalized, targetDistance, copier.obstacleLayer);
        
        Vector2 finalPos;

        if (hit.collider != null)
        {
            // [Modified] To prevent spawning in the wall without BoxCast, we calculate the object's extent 
            // along the ray direction and offset the position back from the hit point.
            Vector2 extents = copier.MemorizedCollider.bounds.extents;
            float projection = (Mathf.Abs(dirNormalized.x) * extents.x) + (Mathf.Abs(dirNormalized.y) * extents.y);

            finalPos = hit.point - (dirNormalized * projection);
        }
        else
        {
            finalPos = playerPos + (dirNormalized * targetDistance); 
        }

        if (copier.useGridSnap)
        {
            finalPos.x = Mathf.Round(finalPos.x / copier.gridSize) * copier.gridSize;
            finalPos.y = Mathf.Round(finalPos.y / copier.gridSize) * copier.gridSize;
        }

        return finalPos;
    }
}