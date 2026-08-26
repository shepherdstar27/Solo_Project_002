using System;
using UnityEngine;

[Serializable]
public class TruckWheel
{
    public WheelCollider WheelCollider_Wheel;
    public Transform Transform_WheelMesh;
    public bool IsSteerWheel;
    public bool IsDriveWheel;

    public void UpdateMeshTransform()
    {
        if (WheelCollider_Wheel == null || Transform_WheelMesh == null)
        {
            return;
        }

        Vector3 position;
        Quaternion rotation;
        WheelCollider_Wheel.GetWorldPose(out position, out rotation);

        Transform_WheelMesh.position = position;
        Transform_WheelMesh.rotation = rotation;
    }
}