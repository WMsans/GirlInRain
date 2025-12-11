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

        // 1. Clamp distance to max range
        float targetDistance = Mathf.Min(distanceToMouse, copier.maxPasteDistance);
        Vector2 targetPos = playerPos + (direction.normalized * targetDistance);

        // 2. Check for obstacles (Line of Sight)
        RaycastHit2D hit = Physics2D.BoxCast(playerPos, copier.MemorizedCollider.bounds.size, 0, direction.normalized, targetDistance, copier.obstacleLayer);
        
        if (hit.collider != null)
        {
            return hit.centroid;
        }

        return targetPos; 
    }
}
