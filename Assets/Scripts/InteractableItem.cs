using UnityEngine;
using TMPro;

public class InteractableItem : MonoBehaviour
{
    [SerializeField] private float interactionDistance = 3f;
    [SerializeField] private Vector3 holdOffset = new Vector3(0, -0.5f, 1f);

    private bool isHighlighted = false;
    public bool isHeld = false;
    
    private Rigidbody rb;
    private Vector3 originalScale;
    private Transform originalParent;
    public TextMeshProUGUI interactText;
    public Transform playerCamera;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerCamera = Camera.main.transform;
        interactText = GameObject.FindGameObjectWithTag("interactText")?.GetComponent<TextMeshProUGUI>();
        rb = GetComponent<Rigidbody>();
        originalScale = transform.localScale;
        originalParent = transform.parent;

        if (interactText != null)
        {
            interactText.enabled = false;
        }

    }

    // Update is called once per frame
    void Update()
    {
        if (isHeld)
        {
            // Check if player releases E
            if (Input.GetKeyUp(KeyCode.E))
            {
                DropItem();
            }
        }
    }

    public void Highlight(bool highlight)
    {
        if (isHeld) return;
        
        isHighlighted = highlight;
        if (interactText != null)
        {
            interactText.enabled = highlight;
            interactText.text = "Hold E to pick up";
        }
    }

    public void PickUp(Transform newParent)
    {
        if (isHeld) return;
        
        // Store the current world position and rotation
        Vector3 worldPosition = transform.position;
        Quaternion worldRotation = transform.rotation;
        
        // Parent to the new parent (camera/hand)
        transform.SetParent(newParent, true); // Keep world position and rotation
        
        // Explicitly set the position and rotation to maintain them
        transform.position = worldPosition;
        transform.rotation = worldRotation;
        
        isHeld = true;
        isHighlighted = false;
        
        if (rb)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
        
        if (interactText != null)
        {
            interactText.enabled = false;
        }
    }

    private void DropItem()
    {
        isHeld = false;
        
        if (rb)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }
        
        transform.SetParent(originalParent);
    }

    public bool IsHighlighted => isHighlighted;
}
