using UnityEngine;
using Unity.Netcode;

[CreateAssetMenu(fileName = "DrillBoxData", menuName = "Drill/DrillBoxData")]
public class DrillBoxData : ScriptableObject
{
    public string drillPartName;
    public int coalCount;
    public int ironCount;
    public int goldCount;
    public NetworkObject dropPrefab; // ✅ NetworkObject, not GameObject
}