using UnityEngine;

public class GameManager : MonoBehaviour
{
    // This class manages the game state, including starting and ending the game, and handling player interactions.
    public static GameManager instance;
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // Keep this instance across scenes
        }
        else
        {
            Destroy(gameObject); // Ensure only one instance exists
        }
    }

    // create a reference to the coal ui tmpro text
    public TMPro.TextMeshProUGUI coalCountText;
    // store the number of coal collected
    public int coalCollected = 50;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public void CollectCoal(int amount)
    {
        coalCollected += amount;
        coalCountText.text = "Fuel: " + coalCollected;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
