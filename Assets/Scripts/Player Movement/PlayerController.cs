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
    public CapsuleCollider2D col;
    public PlayerStats stats;
    [Header("States")]
    public StateMachineRunner stateMachine;
    public List<PlayerState> states;
    [Header("Info")]
    public int AirDashesLeft { get; private set; }

    public void UseAirDash() => AirDashesLeft--;
    public void ResetAirDashes() => AirDashesLeft = stats.maxAirDashes;
    public float FrameLeftGrounded { get; private set; } 
    public void UpdateFrameLeftGrounded() => FrameLeftGrounded = Time.time;

    private void Start()
    {
        FrameLeftGrounded = Time.time;
    }
    private void OnDrawGizmosSelected()
    {
        if(!stats) return;
        var cap = col as CapsuleCollider2D;
        if(!cap) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(col.bounds.center + (stats.grounderDistance + cap.size.y/2) * Vector3.down, .05f);
    }

    public bool GetGrounded()
    {
        return Physics2D.CapsuleCast(col.bounds.center, col.size, col.direction, 0, Vector2.down, stats.grounderDistance, stats.groundLayer);
    }
}
