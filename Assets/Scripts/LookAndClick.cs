using UnityEngine;
using UnityEngine.InputSystem;

public class LookAndClickInteraction : MonoBehaviour
{
    public Camera playerCamera;                // assign your FPS camera in Inspector
    public float interactRange = 1f;           // how far you can look and interact
    public InputActionReference clickAction;   // assign your "Click" action

    public InputActionReference eButtonAction;   // assign your "E button" action

    // Variables to track mining state and reset if let go
    private MineType mineType;
    private MineType currentMineTarget;

    public void Start()
    {
        mineType = FindObjectOfType<MineType>();
    }

    void OnEnable()
    {
        clickAction.action.Enable();
        eButtonAction.action.Enable();
    }

    void OnDisable()
    {
        clickAction.action.Disable();
        eButtonAction.action.Disable();
    }

    
    void Update()
    {
        if (eButtonAction.action.WasPressedThisFrame())
        { 
            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, interactRange))
            {
                Dropped dropScript = hit.collider.GetComponent<Dropped>();
                if (dropScript != null)
                {
                    dropScript.PickedUp(hit.collider.gameObject);
                }


            }
        }

        if (clickAction.action.WasReleasedThisFrame())
        {
            if (currentMineTarget != null)
            {
                currentMineTarget.holdCount = 0;
                Debug.Log("Stopped Mining Cube");
                currentMineTarget = null; // clear reference
            }
        }

        if (clickAction.action.IsPressed())
        {
            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, interactRange))
            {
                MineType mineTypeScript = hit.collider.GetComponent<MineType>();
                if (mineTypeScript != null)
                {
                    currentMineTarget = mineTypeScript;
                    mineTypeScript.Mining();
                }


            }
        }
    }

}
