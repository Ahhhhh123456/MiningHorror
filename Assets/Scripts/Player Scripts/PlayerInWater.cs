using UnityEngine;

public class PlayerInWater : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        Swimming swimmingScript = other.GetComponentInParent<Swimming>();
        if (swimmingScript == null) return;

        swimmingScript.isSwimming = true;
        swimmingScript.rb.useGravity = false;
        Debug.Log("Player entered water, swimming enabled");

        // // Only start drowning if it's the head
        // if (other == swimmingScript.head)
        // {
        //     swimmingScript.isDrowning = true;
        //     Debug.Log("Player head submerged, start drowning");
        // }
    }


    private void OnTriggerExit(Collider other)
    {
        Swimming swimmingScript = other.GetComponentInParent<Swimming>();
        if (swimmingScript == null) return;

        // Stop swimming only when **all colliders** leave water

        swimmingScript.isSwimming = false;
        swimmingScript.rb.useGravity = true;
        swimmingScript.drowningTimer = 0f;


        // Stop drowning if head leaves
        // if (other == swimmingScript.head)
        // {
        //     swimmingScript.isDrowning = false;
        // }
    }
}
