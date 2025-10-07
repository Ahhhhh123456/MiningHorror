using Unity.Netcode;
using UnityEngine;

public class NetworkedBoxData : NetworkBehaviour
{
    // NetworkVariables for ore counts
    public NetworkVariable<int> stoneCount = new NetworkVariable<int>(0);
    public NetworkVariable<int> ironCount = new NetworkVariable<int>(0);
    public NetworkVariable<int> goldCount = new NetworkVariable<int>(0);

    public void InitializeFromDrillBoxData(DrillBoxData boxData)
    {

        // Set starting values
        stoneCount.Value = boxData.stoneCount;
        ironCount.Value = boxData.ironCount;
        goldCount.Value = boxData.goldCount;
    }
}
