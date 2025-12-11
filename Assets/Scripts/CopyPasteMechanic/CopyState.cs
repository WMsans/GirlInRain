using UnityEngine;
using UnityEngine.InputSystem;

public class CopyState : State
{
    public override void OnUpdate(StateMachineRunner runner)
    {
        // Cast runner to Copier to access specific data
        Copier copier = runner as Copier;
        if (copier == null) return;

        // Interaction Logic (Destroy / Clear All)
        copier.HandleInteractionLogic();

        // Switch to Paste State
        if (((Copier)runner).inputSystemActions.Player.Crouch.WasPressedThisFrame()) 
        {
            runner.ChangeState(copier.pasteState);
            return;
        }

        // Copy Logic
        if (((Copier)runner).inputSystemActions.Player.Attack.WasPressedThisFrame()) 
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.value);
            
            // Check for collider under mouse
            Collider2D hit = Physics2D.OverlapPoint(mousePos);
            if (hit != null)
            {
                CopiableObject copiable = hit.GetComponent<CopiableObject>();
                if (copiable != null)
                {
                    copier.Memorize(copiable.GetSourcePrefab(), copiable.energyCost);
                }
            }
        }
    }
}
