using UnityEngine;

public class StateMachineRunner : MonoBehaviour
{
    public State CurrentState{ get; private set;}
    [SerializeField] private State initialState;

    private void Start()
    {
        ChangeState(initialState);
    }
    public void ChangeState(State newState)
    {
        if(CurrentState != null)
            CurrentState.OnExit(this);
        CurrentState = newState;
        CurrentState.OnEnter(this);
    }

    private void Update()
    {
        CurrentState?.OnUpdate(this);
    }
    private void FixedUpdate()
    {
        CurrentState?.OnFixedUpdate(this);
    }
    private void LateUpdate()
    {
        CurrentState?.OnLateUpdate(this);
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        CurrentState?.CollisionEnter(collision, this);
    }
    private void OnCollisionStay2D(Collision2D collision)
    {
        CurrentState?.CollisionStay(collision, this);
    }
    private void OnCollisionExit2D(Collision2D collision)
    {
        CurrentState?.CollisionExit(collision, this);
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        CurrentState?.TriggerEnter(other, this);
    }
    private void OnTriggerStay2D(Collider2D other)
    {
        CurrentState?.TriggerStay(other, this);
    }
    private void OnTriggerExit2D(Collider2D other)
    {
        CurrentState?.TriggerExit(other, this);
    }
}
