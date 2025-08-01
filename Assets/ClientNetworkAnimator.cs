using Unity.Netcode.Components;

public class ClientNetworkAnimator : NetworkAnimator 
{
    // // Start is called once before the first execution of Update after the MonoBehaviour is created
    // void Start()
    // {
        
    // }

    // // Update is called once per frame
    // void Update()
    // {
        
    // }

    protected override bool OnIsServerAuthoritative()
    {
        return false;
    }
}
