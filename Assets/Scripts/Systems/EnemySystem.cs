using Unity.Burst;
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
    protected override void OnCreate()
    {
        ecbSystem = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        var ecb = ecbSystem.CreateCommandBuffer().AsParallelWriter();
        float deltaTime = SystemAPI.Time.DeltaTime;

        Entity character = GameManager.GetEntityForTag("Character");
        float3 characerPos = character == Entity.Null ? float3.zero : EntityManager.GetComponentData<LocalToWorld>(character).Position;

        characerPos.y = 0;
        ComponentLookup<Character> characterData = GetComponentLookup<Character>();

        Entities
            .ForEach((ref EnemyAspects enemy, in int entityInQueryIndex, in Entity entity) =>
            {
                if(enemy.enemy.ValueRO.hp <= 0)
                {
                    if (enemy.enemy.ValueRO.animatior != EnemyAnimatior.Dead)
                        ecb.AddComponent(entityInQueryIndex, entity, new AutoDestory { destoryTime = enemy.enemy.ValueRO.deadStayTime });
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
                    //Character data = characterData[character];
                    //data.hp -= enemy.enemy.ValueRO.damage;
                    //characterData[character] = data;
                }
                else if(dis < enemy.enemy.ValueRO.runSize)
                {
                    newAnimator = EnemyAnimatior.Run;
                    speed = enemy.enemy.ValueRO.runSpeed;
                }
                else if(dis < enemy.enemy.ValueRO.walkSize)
                {
                    newAnimator = EnemyAnimatior.Walk;
                    speed = enemy.enemy.ValueRO.walkSpeed;
                }
                //修改动画标识
                enemy.enemy.ValueRW.animatior = newAnimator;

                //修改移动速度
                enemy.velocity.ValueRW.Linear = linear * speed;

            })
            .ScheduleParallel();
        ecbSystem.AddJobHandleForProducer(Dependency);
    }
}