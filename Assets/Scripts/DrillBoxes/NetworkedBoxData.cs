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

        // Set starting values
        coalCount.Value = boxData.coalCount;
        ironCount.Value = boxData.ironCount;
        goldCount.Value = boxData.goldCount;
    }
}
