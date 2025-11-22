using Unity.VisualScripting;
using UnityEngine;

public class warden1AnimsTest : MonoBehaviour
{
    public Animator animator;

    [SerializeField]
    warden1States warden1state = new warden1States();
    public enum warden1States
    {
        IDLE,
        WALK,
        RUN,
        SCREAM,
        HIT,
        ATTACK,
    }

    warden1States CurrentState
    {
        set
        {
            warden1CurrentState = value;

            switch(warden1CurrentState)
            {
                case warden1States.IDLE:
                    animator.Play("Idle");
                    break;
                case warden1States.WALK:
                    animator.Play("Walk");
                    break;
                case warden1States.RUN:
                    animator.Play("Run");
                    break;
                case warden1States.SCREAM:
                    animator.Play("Scream");
                    break;
                case warden1States.HIT:
                    animator.Play("Hit");
                    break;
                case warden1States.ATTACK:
                    animator.Play("Attack");
                    break;
            }
        }
    }

    warden1States warden1CurrentState;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        animator = GetComponentInChildren<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        if (warden1CurrentState != warden1state)
        {
            CurrentState = warden1state;
        }
    }
}