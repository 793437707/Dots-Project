using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

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
            .ForEach((AutoDestoryAspects autoDestory, int entityInQueryIndex) =>
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