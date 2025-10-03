using UnityEngine;
using TMPro;

public class HoverText : MonoBehaviour
{
    [Header("UI")]
    public Canvas playerCanvas;        // assign a canvas prefab
    public TextMeshProUGUI hoverText;  // assign a TMP Text prefab

    [Header("Hover Settings")]
    public string objectName = "Item"; // name to show
    public float hoverOffset = 2f;     // world units above the object

    private Camera playerCamera;

    void Start()
    {
        playerCanvas.gameObject.SetActive(true); // hide by default
        playerCamera = Camera.main;               // or assign per player if needed
    }

    void Update()
    {
        CheckHover();
    }

    private void CheckHover()
    {
        // Raycast from camera
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, 5f)) // max distance optional
        {
            if (hit.collider.gameObject == gameObject)
            {
                ShowHover();
                return;
            }
        }

        HideHover();
    }

    private void ShowHover()
    {
        if (!playerCanvas.gameObject.activeSelf)
            playerCanvas.gameObject.SetActive(true);

        hoverText.text = objectName;

        // position canvas above object
        Vector3 worldPos = transform.position + Vector3.up * hoverOffset;
        Vector3 screenPos = playerCamera.WorldToScreenPoint(worldPos);
        playerCanvas.transform.position = screenPos;
    }

    private void HideHover()
    {
        if (playerCanvas.gameObject.activeSelf)
            playerCanvas.gameObject.SetActive(false);
    }
}
