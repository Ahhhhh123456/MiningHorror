using UnityEngine;
using UnityEngine.InputSystem; // Required for new system

public class MouseInputExample : MonoBehaviour
{
    public InputActionReference clickAction;  

    void OnEnable()
    {
        clickAction.action.Enable();
    }

    void OnDisable()
    {
        clickAction.action.Disable();
    }

    void Update()
    {

        // Mouse click
        // if (clickAction.action.WasPressedThisFrame())
        // {
        //     Debug.Log("Mouse button pressed down");
        // }

        // Fires only when button is released
        // if (clickAction.action.WasReleasedThisFrame())
        // {
        //     Debug.Log("Mouse button released");
        // }

        // True every frame the button is being held
        // if (clickAction.action.IsPressed())
        // {
        //     Debug.Log("Mouse button is being held");
        // }
    }
}
