using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class PlayerController : MonoSingleton<PlayerController>
{
    public Rigidbody2D rb;
    public Animator animator;
    public Collider2D col;
    public PlayerStats stats;
    [Header("States")]
    public StateMachineRunner stateMachine;
    public List<PlayerState> states;

    public float FrameLeftGrounded { get; private set; } 
    public void UpdateFrameLeftGrounded() => FrameLeftGrounded = Time.time;

    private void Start()
    {
        FrameLeftGrounded = Time.time;
    }
    private void OnDrawGizmosSelected()
    {
        if(!stats) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(col.bounds.center + (stats.grounderDistance + col.bounds.size.y/2) * Vector3.down, .05f);
    }

    public bool GetGrounded()
    {
        return Physics2D.BoxCast(col.bounds.center, col.bounds.size, 0, Vector2.down, stats.grounderDistance, stats.groundLayer);
    }
}
