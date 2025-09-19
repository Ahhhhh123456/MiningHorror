using UnityEngine;

public class Swimming : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // Check if the player entered the water
        PlayerMovement player = other.GetComponent<PlayerMovement>();
        if (player != null)
        {
            player.isSwimming = true;
            player.rb.useGravity = false;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerMovement player = other.GetComponent<PlayerMovement>();
        if (player != null)
        {
            player.isSwimming = false;
            player.rb.useGravity = true;
        }
    }
}
