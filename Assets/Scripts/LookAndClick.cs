using UnityEngine;
using UnityEngine.InputSystem;

public class LookAndClickInteraction : MonoBehaviour
{
    public Camera playerCamera;                // assign your FPS camera in Inspector
    public float interactRange = 1f;           // how far you can look and interact
    public InputActionReference clickAction;   // assign your "Click" action

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
        if (clickAction.action.IsPressed())
        {
            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, interactRange))
            {
                MineType mineTypeScript = hit.collider.GetComponent<MineType>();
                if (mineTypeScript != null)
                {
                    mineTypeScript.Mining(); // don’t pass item, it already knows itself
                }
                
    
            }
        }

        if (clickAction.action.WasReleasedThisFrame())
        {
            Debug.Log("Stopped Mining Cube");
        }
    }

}
