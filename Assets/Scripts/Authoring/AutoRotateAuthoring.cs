using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

class AutoRotateAuthoring : MonoBehaviour
{
    [Tooltip("XYZ对应的float3为(1,0,0),(0,1,0),(0,0,1)")]
    public float3 RotationAxis;
    public float RotationSpeed;
}

class AutoRotateBaker : Baker<AutoRotateAuthoring>
{
    public override void Bake(AutoRotateAuthoring authoring)
    {
        var e = GetEntity(authoring, TransformUsageFlags.Dynamic);
        AddComponent(e, new AutoRotate
        {
            RotationAxis = authoring.RotationAxis,
            RotationSpeed = authoring.RotationSpeed
        });
    }
}