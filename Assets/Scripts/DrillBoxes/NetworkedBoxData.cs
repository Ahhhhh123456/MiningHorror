using Unity.Netcode;
using UnityEngine;

public class NetworkedBoxData : NetworkBehaviour
{
    // NetworkVariables for ore counts
public NetworkVariable<int> coalCount = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> ironCount = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> goldCount = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public void InitializeFromDrillBoxData(DrillBoxData boxData)
    {

        if (!IsServer) return; // Only the server should modify NetworkVariables

        int playerCount = NetworkManager.Singleton.ConnectedClients.Count;

        // multiply base counts by number of connected players
        coalCount.Value = boxData.coalCount * playerCount;
        ironCount.Value = boxData.ironCount * playerCount;
        goldCount.Value = boxData.goldCount * playerCount;

    }
}
