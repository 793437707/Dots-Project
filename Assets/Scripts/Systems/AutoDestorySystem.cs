using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[BurstCompile]
[UpdateInGroup(typeof(PresentationSystemGroup))]
partial class AutoDestorySystem : SystemBase
{
    private EntityCommandBufferSystem ecbSystem;
    protected override void OnCreate()
    {
        ecbSystem = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        var ecb = ecbSystem.CreateCommandBuffer().AsParallelWriter();
        float deltaTime = SystemAPI.Time.DeltaTime;

        Entities
            .ForEach((ref AutoDestoryAspects autoDestory, in int entityInQueryIndex) =>
            {
                autoDestory.lastTime -= deltaTime;
                if(autoDestory.lastTime < 0f)
                {
                    ecb.DestroyEntity(entityInQueryIndex, autoDestory.self);
                }
            })
            .ScheduleParallel();
        ecbSystem.AddJobHandleForProducer(Dependency);
    }
}