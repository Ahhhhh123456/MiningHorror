using UnityEngine;

public class warden1Animator : MonoBehaviour
{
    public Animator animator;

    public enum Warden1State
    {
        IDLE,
        WALK
    }

    private Warden1State currentState;

    Warden1State CurrentState
    {
        get => currentState;
        set
        {
            if (currentState == value) return; // prevent replaying same animation

            currentState = value;

            switch (currentState)
            {
                case Warden1State.IDLE:
                    animator.Play("Idle");
                    break;
                case Warden1State.WALK:
                    animator.Play("Walk");
                    break;
            }
        }
    }

    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        CurrentState = Warden1State.IDLE;
    }

    public void SetIdle()
    {
        CurrentState = Warden1State.IDLE;
    }

    public void SetWalk()
    {
        CurrentState = Warden1State.WALK;
    }
}
