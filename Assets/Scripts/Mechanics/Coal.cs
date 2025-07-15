using UnityEngine;

public class Coal : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Logic for when the player collects coal
            Debug.Log("Coal collected!");
            GameManager.instance.coalCollected+=5; // Increment the coal count in the GameManager
            GameManager.instance.coalCountText.text = "Fuel: " + GameManager.instance.coalCollected; // Update the UI text
            Destroy(gameObject); // Destroy the coal object
        }
    }
}
