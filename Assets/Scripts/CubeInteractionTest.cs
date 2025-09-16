using UnityEngine;
using UnityEngine.InputSystem;

public class CubeInteractionTest : MonoBehaviour
{
    public InputActionReference clickAction; // drag in your "Click" action from the Input Actions asset
    private Renderer cubeRenderer;
    private Color originalColor;
    public Color holdColor = Color.red;

    public int holdCount = 0;

    void OnEnable()
    {
        clickAction.action.Enable();
    }

    void OnDisable()
    {
        clickAction.action.Disable();
    }

    void Start()
    {
        cubeRenderer = GetComponent<Renderer>();
        originalColor = cubeRenderer.material.color;
    }

    void Update()
    {
        // If left mouse button is held down

        if (clickAction.action.IsPressed())
        {
            holdCount += 1;
            if (holdCount == 50)
            {
                Debug.Log("Mining Cube");
                cubeRenderer.material.color = holdColor; // change color while holding
                holdCount = 0;
            }
        }

        if (clickAction.action.WasReleasedThisFrame() && holdCount != 50)
        {
            Debug.Log("Stopped Mining Cube");
            holdCount = 0;
            cubeRenderer.material.color = originalColor; // revert when released
        }
    }
}
