using Unity.Entities;
using Unity.Mathematics;

public struct AutoRotate : IComponentData
{
    public float3 RotationAxis;
    public float RotationSpeed;
}
