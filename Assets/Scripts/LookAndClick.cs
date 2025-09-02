using UnityEngine;
using UnityEngine.InputSystem;

public class LookAndClickInteraction : MonoBehaviour
{
    public Camera playerCamera;                // assign your FPS camera in Inspector
    public float interactRange = 1f;           // how far you can look and interact
    public InputActionReference clickAction;   // assign your "Click" action

    public int holdCount = 0;

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
        // Only run if button is held
        if (clickAction.action.IsPressed())
        {
            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, interactRange))
            {
                if (hit.collider.CompareTag("Cube"))
                {
                    //hit.collider.GetComponent<Renderer>().enabled = false;
                    holdCount += 1;
                    Debug.Log(holdCount);
                    if (holdCount == 100)
                    {
                        Debug.Log("Mining Cube");
                        hit.collider.gameObject.SetActive(false);
                        holdCount = 0;
                    }

                }
            }
        }

        if (clickAction.action.WasReleasedThisFrame() && holdCount != 100)
        {
            Debug.Log("Stopped Mining Cube");
            holdCount = 0;
        }

    }
}
