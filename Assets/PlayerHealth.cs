using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private int health = 100;
    void Start()
    {
        Debug.Log("PlayerHealth script started. Initial health: " + health);
    }


}
