using UnityEngine;
using TMPro;
using Unity.Netcode;

public class HoverText : MonoBehaviour
{
    public Camera mainCamera;
    public TextMeshProUGUI hoverText;
    public LayerMask interactableLayer;

    void Update()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 5f, interactableLayer))
        {
            hoverText.gameObject.SetActive(true);

            NetworkedBoxData box = hit.collider.GetComponent<NetworkedBoxData>();
            if (box != null)
            {
                hoverText.text = hit.collider.gameObject.name + "\n" +
                                 $"Coal: {box.coalCount.Value}\n" +
                                 $"Iron: {box.ironCount.Value}\n" +
                                 $"Gold: {box.goldCount.Value}";
            }
            else
            {
                hoverText.text = hit.collider.gameObject.name;
            }
        }
        else
        {
            hoverText.gameObject.SetActive(false);
        }
    }
}