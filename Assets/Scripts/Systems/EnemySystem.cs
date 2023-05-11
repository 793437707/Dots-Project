using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Windows;

[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
partial class EnemySystem : SystemBase
{
    private EntityCommandBufferSystem ecbSystem;
    private static EntityQuery query;
    protected override void OnCreate()
    {
        ecbSystem = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
        var queryDescription = new EntityQueryDesc
        {
            None = new ComponentType[] { ComponentType.ReadOnly<AutoDestory>() },
            All = new ComponentType[]{ ComponentType.ReadOnly<Enemy>() }
        };
        query = GetEntityQuery(queryDescription);
    }


    protected override void OnUpdate()
    {
        var ecb = ecbSystem.CreateCommandBuffer().AsParallelWriter();
        float deltaTime = SystemAPI.Time.DeltaTime;

        //获取玩家坐标
        Entity character = GameManager.GetEntityForTag("Character");
        float3 characerPos = character == Entity.Null ? float3.zero : EntityManager.GetComponentData<LocalToWorld>(character).Position;
        characerPos.y = 0;
        //修改血量所需的NativeArray
        int dataCount = query.CalculateEntityCount();
        NativeArray<int> attack = new NativeArray<int>(dataCount, Allocator.TempJob);
        Entities
            .WithNone<AutoDestory>()
            .ForEach((ref EnemyAspects enemy, in int entityInQueryIndex, in Entity entity) =>
            {
                if(enemy.enemy.ValueRO.hp <= 0)
                {
                    //怪物死亡：理论上不会第二次进入foreach，因为已经不符合Aspect，速度被删了
                    if (enemy.enemy.ValueRO.animatior != EnemyAnimatior.Dead)
                    {
                        enemy.enemy.ValueRW.animatior = EnemyAnimatior.DeadFirst;
                        ecb.AddComponent(entityInQueryIndex, entity, new AutoDestory { destoryTime = enemy.enemy.ValueRO.deadStayTime, score = 10});
                        ecb.RemoveComponent<PhysicsCollider>(entityInQueryIndex, entity);
                        ecb.RemoveComponent<PhysicsVelocity>(entityInQueryIndex, entity);
                    }
                    return;
                }
                //修改朝向
                float3 selfPos = enemy.transform.ValueRO.Position;
                selfPos.y = 0;
                enemy.localTransform.ValueRW.Rotation = Quaternion.LookRotation(math.normalize(characerPos - selfPos));

                float dis = math.distance(characerPos, selfPos);
                EnemyAnimatior newAnimator = EnemyAnimatior.Idle;
                float3 linear = enemy.localTransform.ValueRO.Forward();
                float speed = 0;

                //正在攻击
                if (enemy.enemy.ValueRO.attackTime > 0)
                {
                    enemy.enemy.ValueRW.attackTime -= deltaTime;
                    newAnimator = EnemyAnimatior.Attack;
                }
                //在攻击范围切没在攻击
                else if (dis < enemy.enemy.ValueRO.attackSize)
                {
                    enemy.enemy.ValueRW.attackTime = enemy.enemy.ValueRO.attackCD;
                    newAnimator = EnemyAnimatior.AttackFirst;
                    //扣玩家血
                    attack[entityInQueryIndex] = enemy.enemy.ValueRO.damage;
                }
                else if(dis < enemy.enemy.ValueRO.runSize || enemy.enemy.ValueRO.hp < enemy.enemy.ValueRO.maxHp)
                {
                    newAnimator = EnemyAnimatior.Run;
                    speed = enemy.enemy.ValueRO.runSpeed;
                }
                else if(dis < enemy.enemy.ValueRO.walkSize)
                {
                    newAnimator = EnemyAnimatior.Walk;
                    speed = enemy.enemy.ValueRO.walkSpeed;
                }
                else if(dis <enemy.enemy.ValueRO.maxSizeForDead)
                {
                    newAnimator = EnemyAnimatior.Walk;
                    speed = enemy.enemy.ValueRO.maxSizeWalkSpeed;
                }
                else
                {
                    ecb.AddComponent(entityInQueryIndex, entity, new AutoDestory { destoryTime = -1, score = 0 });
                }
                //修改动画标识
                enemy.enemy.ValueRW.animatior = newAnimator;

                //修改移动速度
                enemy.velocity.ValueRW.Linear = linear * speed;

            })
            .ScheduleParallel();

        //修改血量
        Entities
            .WithAll<Character>()
            .ForEach(() =>
            {
                int sum = 0;
                for(int i = 0; i < attack.Length; i++)
                {
                    sum += attack[i];
                }
                CharacterData.Inst.hp -= sum * CharacterData.Inst.GetDamage / 100;
                CharacterData.Inst.hp = math.max(0, CharacterData.Inst.hp);
            })
            .WithDisposeOnCompletion(attack)
            .WithoutBurst()
            .Schedule();

        ecbSystem.AddJobHandleForProducer(Dependency);
    }
}