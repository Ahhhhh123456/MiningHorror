using UnityEngine;

public class ChainBehavior : MonoBehaviour
{
    public Rigidbody playerA;
    public Rigidbody playerB;
    public float maxDistance = 2f;

    void FixedUpdate()
    {
        Vector3 offset = playerB.position - playerA.position;
        float distance = offset.magnitude;

        if (distance > maxDistance)
        {
            Vector3 correctionDir = offset.normalized;
            float correction = distance - maxDistance;

            playerA.MovePosition(playerA.position + correctionDir * (correction * 0.5f));
            playerB.MovePosition(playerB.position - correctionDir * (correction * 0.5f));
        }
    }
}
