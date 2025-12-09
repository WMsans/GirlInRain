using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class State : ScriptableObject
{
    public virtual void OnEnter(StateMachineRunner runner){}
    public virtual void OnExit(StateMachineRunner runner){}
    public virtual void OnUpdate(StateMachineRunner runner){}
    public virtual void OnFixedUpdate(StateMachineRunner runner){}
    public virtual void OnLateUpdate(StateMachineRunner runner){}
    public virtual void CollisionEnter(Collision2D collision, StateMachineRunner runner){}
    public virtual void CollisionStay(Collision2D collision, StateMachineRunner runner){}
    public virtual void CollisionExit(Collision2D collision, StateMachineRunner runner){}
    public virtual void TriggerEnter(Collider2D other, StateMachineRunner runner){}
    public virtual void TriggerStay(Collider2D other, StateMachineRunner runner){}
    public virtual void TriggerExit(Collider2D other, StateMachineRunner runner){}
}
