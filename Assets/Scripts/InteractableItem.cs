using UnityEngine;
using TMPro;

public class InteractableItem : MonoBehaviour
{
    [SerializeField] private Vector3 holdOffset = new Vector3(0, -0.5f, 1f); // Offset when held

    private bool isHighlighted = false;
    public bool isHeld = false;

    private Rigidbody rb;
    private Transform originalParent;
    public TextMeshProUGUI interactText;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        originalParent = transform.parent;

        // Optionally assign a UI element if you have one in the scene
        interactText = GameObject.FindGameObjectWithTag("interactText")?.GetComponent<TextMeshProUGUI>();
        if (interactText != null)
            interactText.enabled = false;
    }

    // Called by PlayerInteract to show/hide highlight UI
    public void Highlight(bool highlight)
    {
        if (isHeld) return;

        isHighlighted = highlight;
        if (interactText != null)
        {
            interactText.enabled = highlight;
            if (highlight)
                interactText.text = "Press E to pick up";
        }
    }

    // Called by PlayerInteract when E is pressed
    public void PickUpItem(Transform newParent)
    {
        if (isHeld) return;

        transform.SetParent(newParent);
        transform.localPosition = holdOffset;
        transform.localRotation = Quaternion.identity;

        isHeld = true;
        isHighlighted = false;

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        if (interactText != null)
            interactText.enabled = false;
    }

    // Called by PlayerInteract when E is released
    public void DropItem()
    {
        isHeld = false;

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }

        transform.SetParent(originalParent);
    }

    public bool IsHighlighted => isHighlighted;
}
