using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[BurstCompile]
[UpdateInGroup(typeof(PresentationSystemGroup))]
partial class AutoDestorySystem : SystemBase
{
    private EntityCommandBufferSystem ecbSystem;
    private static EntityQuery query;
    protected override void OnCreate()
    {
        ecbSystem = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
        query = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<AutoDestory>());
    }

    protected override void OnUpdate()
    {
        if (!GameManager.inGame)
            return;
        var ecb = ecbSystem.CreateCommandBuffer().AsParallelWriter();
        float deltaTime = SystemAPI.Time.DeltaTime;

        int dataCount = query.CalculateEntityCount();
        NativeArray<int> score = new NativeArray<int>(dataCount, Allocator.TempJob);

        Entities
            .ForEach((ref AutoDestoryAspects autoDestory, in int entityInQueryIndex) =>
            {
                //死亡瞬间加分，只加一次
                if(autoDestory.autoDestory.ValueRO.added == false)
                {
                    autoDestory.autoDestory.ValueRW.added = true;
                    score[entityInQueryIndex] = autoDestory.autoDestory.ValueRO.score;
                }
                autoDestory.lastTime -= deltaTime;
                if(autoDestory.lastTime < 0f)
                {
                    ecb.DestroyEntity(entityInQueryIndex, autoDestory.self);
                }
            })
            .ScheduleParallel();

        //修改积分
        Job
            .WithCode(() =>
            {
                int sum = 0;
                for (int i = 0; i < score.Length; i++)
                {
                    sum += score[i];
                }
                WorldData.Inst.totalScore += sum;
            })
            .WithDisposeOnCompletion(score)
            .WithoutBurst()
            .Schedule();

        ecbSystem.AddJobHandleForProducer(Dependency);
    }
}