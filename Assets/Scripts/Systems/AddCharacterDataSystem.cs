using System.Collections;
using System.Numerics;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
partial class AddCharacterDataSystem : SystemBase
{
    private EntityCommandBufferSystem ecbSystem;
    public static float3 DestoryPos = new float3(-10000, -1000, -10000);
    protected override void OnCreate()
    {
        ecbSystem = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
    }
    protected override void OnUpdate()
    {
        if (!GameManager.inGame)
            return;
        var ecb = ecbSystem.CreateCommandBuffer().AsParallelWriter();
        float deltaTime = SystemAPI.Time.DeltaTime;
        //获取玩家坐标
        Entity character = GameManager.GetEntityForTag("Character");
        float3 characerPos = character == Entity.Null ? float3.zero : EntityManager.GetComponentData<LocalToWorld>(character).Position;
        characerPos.y = 0;
        Entity boxParent = GameManager.GetEntityForTag("Box");

        Entities
            .ForEach((ref AddCharacterData data, in LocalToWorld transform, in int entityInQueryIndex, in Entity entity) =>
            {
                data.AddCD -= deltaTime;
                if (data.AddCD > 0)
                    return;
                float3 selfPos = transform.Position;
                selfPos.y = 0;
                float dis = math.distance(characerPos, selfPos);
                if (!data.infDis && dis > data.minDis)
                    return;
                data.AddCD = data.nextAddTime;

                //这是一行不能被Burst的代码
                CharacterData.Inst.AddValueByAddEnum(data.type, data.value);

                if(data.AddOnce)
                    ecb.AddComponent(entityInQueryIndex, entity, new AutoDestory { destoryTime = -1, score = 0 });
            })
            .WithoutBurst()
            .ScheduleParallel();

        Entities
            .WithNone<AutoDestory>()
            .ForEach((in Box box, in LocalTransform transform, in int entityInQueryIndex, in Entity entity) =>
            {
                if (box.hp > 0)
                    return;
                ecb.AddComponent(entityInQueryIndex, entity, new AutoDestory { destoryTime = 2, score = 50 });
                ecb.SetComponent(entityInQueryIndex, entity, new PhysicsVelocity
                {
                    Linear = new float3(0, -2, 0)
                });
                if(box.spawnEntity != Entity.Null)
                {
                    Entity spawnEntity = ecb.Instantiate(entityInQueryIndex, box.spawnEntity);
                    ecb.AddComponent(entityInQueryIndex, spawnEntity, new Parent { Value = boxParent });
                    ecb.SetComponent(entityInQueryIndex, spawnEntity, LocalTransform.FromPosition(transform.Position));
                }
                    
            })
            .ScheduleParallel();
        ecbSystem.AddJobHandleForProducer(Dependency);
    }
}