using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.UIElements;

[UpdateInGroup(typeof(InitializationSystemGroup))]
partial class SpawnMobsSystem : SystemBase
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
        bool mouseInput = true;

        Entities.
            ForEach((SpawnMobAspects spawnMobAspects, int entityInQueryIndex) =>
            {
                spawnMobAspects.spawnMobs.ValueRW.nextSpawnTime -= deltaTime;
                if ((spawnMobAspects.spawnMobs.ValueRO.autoSpawn || spawnMobAspects.spawnMobs.ValueRO.mouseSpawn && mouseInput)
                    && spawnMobAspects.spawnMobs.ValueRO.nextSpawnTime < 0)
                {
                    spawnMobAspects.spawnMobs.ValueRW.nextSpawnTime = spawnMobAspects.spawnMobs.ValueRO.spawnCD;

                    var instance = ecb.Instantiate(entityInQueryIndex, spawnMobAspects.spawnMobs.ValueRO.spawnPrefab);

                    ecb.SetComponent(entityInQueryIndex, instance, LocalTransform.FromPositionRotation(
                        spawnMobAspects.spawnTransform.ValueRO.Position, spawnMobAspects.spawnTransform.ValueRO.Rotation));

                    if(spawnMobAspects.spawnMobs.ValueRO.autoMove)
                    {
                        ecb.SetComponent(entityInQueryIndex, instance, new PhysicsVelocity
                        {
                            Linear = spawnMobAspects.spawnTransform.ValueRO.Forward * spawnMobAspects.spawnMobs.ValueRO.moveSpeed
                        }) ;
                    }
                }
            })
            .ScheduleParallel();

        ecbSystem.AddJobHandleForProducer(Dependency);
    }
}
