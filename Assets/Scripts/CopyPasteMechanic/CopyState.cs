using UnityEngine;
using UnityEngine.InputSystem;

public class CopyState : State
{
    [SerializeField] private LayerMask copiableLayerMask;
    public override void OnUpdate(StateMachineRunner runner)
    {
        // Cast runner to Copier to access specific data
        Copier copier = runner as Copier;
        if (copier == null) return;

        // Interaction Logic (Destroy / Clear All)
        copier.HandleInteractionLogic();

        // Switch to Paste State
        if (copier.inputSystemActions.Player.Crouch.WasPressedThisFrame()) 
        {
            runner.ChangeState(copier.pasteState);
            return;
        }

        // Copy Logic
        if (copier.inputSystemActions.Player.Attack.WasPressedThisFrame()) 
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.value);
            
            Collider2D[] hits = Physics2D.OverlapPointAll(mousePos, copiableLayerMask);
            
            foreach (Collider2D hit in hits)
            {
                CopiableObject copiable = hit.GetComponentInParent<CopiableObject>();
                
                // If we found a valid copiable object, memorize it and stop checking
                if (copiable != null)
                {
                    copier.Memorize(copiable.GetSourcePrefab(), copiable.energyCost);
                    return; 
                }
            }
        }
    }
}