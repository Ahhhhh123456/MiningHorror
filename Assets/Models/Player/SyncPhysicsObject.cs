using JetBrains.Rider.Unity.Editor;
using UnityEngine;
using Unity.Netcode;
public class SyncPhysicsObject : MonoBehaviour
{
    Rigidbody rigidbody3D;
    ConfigurableJoint joint;

    [SerializeField] Rigidbody animatedRigidbody3D;

    [SerializeField] public bool syncAnimation = false;

    // Keep track for starting rotation
    Quaternion startLocalRotation;

    void Awake()
    {
        rigidbody3D = GetComponent<Rigidbody>();
        joint = GetComponent<ConfigurableJoint>();

        // Store the starting local rotation
        startLocalRotation = transform.localRotation;

        // Auto-assign the animated Rigidbody if not set in Inspector
        if (animatedRigidbody3D == null)
        {
            animatedRigidbody3D = GetComponent<Rigidbody>();
        }
    }

    // public void UpdateJointFromAnimation()
    // {
    //     if (!syncAnimation)
    //         return;

    //     ConfigurableJointExtensions.SetTargetRotationLocal(joint, animatedRigidbody3D.transform.localRotation, startLocalRotation);
    // }

    public void UpdateJointFromAnimation()
    {
        if (!syncAnimation)
            return;

        // Get the NetworkObject on this ragdoll bone or its parent
        NetworkObject netObj = GetComponentInParent<NetworkObject>();

        // Only the owner should drive animation → prevent spazzing
        if (netObj == null || !netObj.IsOwner)
            return;

        ConfigurableJointExtensions.SetTargetRotationLocal(
            joint,
            animatedRigidbody3D.transform.localRotation,
            startLocalRotation
        );
    }
}
