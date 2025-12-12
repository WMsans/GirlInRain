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

        // Update the Preview Object
        copier.UpdatePreview();

        // Switch to Copy State
        if (copier.inputSystemActions.Player.Crouch.WasPressedThisFrame())
        {
            copier.HidePreview(); // Hide the preview before switching
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
            Vector2 spawnPos = copier.GetPastePosition();

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
}