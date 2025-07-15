using UnityEngine;

public class RopeVisual : MonoBehaviour
{
    public Transform playerA;
    public Transform playerB;

    private LineRenderer line;

    void Start()
    {
        line = GetComponent<LineRenderer>();
        line.positionCount = 2;
    }

    void Update()
    {
        line.SetPosition(0, playerA.position);
        line.SetPosition(1, playerB.position);
    }
}
