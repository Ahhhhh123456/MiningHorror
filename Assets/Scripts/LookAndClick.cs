using UnityEngine;
using UnityEngine.InputSystem;

public class LookAndClickInteraction : MonoBehaviour
{
    public Camera playerCamera;                // assign your FPS camera in Inspector
    public float interactRange = 0.3f;           // how far you can look and interact
    public InputActionReference clickAction;   // assign your "Click" action

    //public int holdCount = 0;

    // [Header("Drop Settings")]
    // public float dropScale = 0.3f;

    // public Vector3 dropOffset = Vector3.up;

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


                if (hit.collider.GetComponent<MeshRenderer>() == true)
                {
                    //hit.collider.GetComponent<Renderer>().enabled = false;

                    MineType mineTypeScript = hit.collider.GetComponent<MineType>();
                    if (mineTypeScript != null)
                    {
                        Debug.Log(mineTypeScript);
                        mineTypeScript.Mining(hit.collider.gameObject);

                        
                    }
                    
                    // holdCount += 1;
                    // Debug.Log(holdCount);
                    // if (holdCount == 100)
                    // {
                        //Debug.Log("Mining Cube");

                        // Drop the item

                        // Dropped dropscript = hit.collider.GetComponent<Dropped>();
                        if (dropscript != null)
                        {
                            dropscript.DropItem(hit.collider.gameObject);
                        }

                        //Destroy(miniObject.GetComponent<LookAndClickInteraction>());

                        hit.collider.gameObject.SetActive(false);
                        //holdCount = 0;
                    // }

                }
            }
        }

        // if (clickAction.action.WasReleasedThisFrame())
        // {
        //     Debug.Log("Stopped Mining Cube");
        //     holdCount = 0;
        // }

    }
}
