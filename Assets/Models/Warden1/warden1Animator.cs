using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

[RequireComponent(typeof(NetworkAnimator))]
public class Warden1Animator : NetworkBehaviour
{
    [Header("Animator References")]
    public Animator animator;                  // The runtime-created Animator (child)
    private NetworkAnimator networkAnimator;   // NetworkAnimator to sync animation

    public enum Warden1State { IDLE, WALK, ATTACK }
    private Warden1State currentState;

    private Warden1State CurrentState
    {
        get => currentState;
        set
        {
            if (currentState == value) return;
            currentState = value;

            // Only server drives animation parameters
            if (IsServer)
            {
                animator.SetBool("isWalking", currentState == Warden1State.WALK);
                animator.SetBool("isAttacking", currentState == Warden1State.ATTACK);
                // NetworkAnimator automatically syncs these parameters to clients
            }
        }
    }

    private void Awake()
    {
        // Find child Animator if not assigned
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        networkAnimator = GetComponent<NetworkAnimator>();

        if (networkAnimator != null && animator != null)
        {
            networkAnimator.Animator = animator; // <-- Assign BEFORE network updates
        }
        else
        {
            Debug.LogError("Warden1Animator: Animator or NetworkAnimator missing!");
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        CurrentState = Warden1State.IDLE; // Set initial state
    }

    #region Public Methods to Control Animation
    public void SetIdle()
    {
        if (!IsServer) return;
        CurrentState = Warden1State.IDLE;
        animator.SetBool("isWalking", false); // server sets this, NetworkAnimator syncs
        animator.SetBool("isAttacking", false); // Ensure attacking is false when idle
    }

    public void SetWalk()
    {
        if (!IsServer) return;
        CurrentState = Warden1State.WALK;
        animator.SetBool("isWalking", true); // server sets this, NetworkAnimator syncs
        animator.SetBool("isAttacking", false); // Ensure attacking is false when walking
    }

    public void SetAttack()
    {
        if (!IsServer) return;
        CurrentState = Warden1State.ATTACK;
        animator.SetBool("isWalking", false); // Ensure walking is false when attacking
        animator.SetBool("isAttacking", true); // server sets this, NetworkAnimator syncs
    }

    #endregion

}
