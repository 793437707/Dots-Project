using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
partial class AutoRotateSystem : SystemBase
{
    protected override void OnUpdate()
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        Entities
            .ForEach((ref LocalTransform localTransform, in AutoRotate spin) =>
            {
                localTransform.Rotate(quaternion.AxisAngle(
                    spin.RotationAxis,
                    math.radians(spin.RotationSpeed * deltaTime)
                ));
            })
            .ScheduleParallel();
    }
}
