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
    protected override void OnCreate()
    {
        ecbSystem = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
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
        NativeArray<int> attack = new NativeArray<int>(1, Allocator.TempJob);
        var GetDamage = CharacterData.Inst.GetDamage;

        Entities
            .ForEach((ref EnemyAspects enemy, in int entityInQueryIndex, in Entity entity) =>
            {
                if(enemy.enemy.ValueRO.hp <= 0)
                {
                    //理论上不会第二次进入foreach，因为已经不符合Aspect，速度被删了
                    if (enemy.enemy.ValueRO.animatior != EnemyAnimatior.Dead)
                    {
                        enemy.enemy.ValueRW.animatior = EnemyAnimatior.DeadFirst;
                        ecb.AddComponent(entityInQueryIndex, entity, new AutoDestory { destoryTime = enemy.enemy.ValueRO.deadStayTime });
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
                    attack[0] += enemy.enemy.ValueRO.damage;
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

        

        //修改血量
        Entities
            .ForEach((ref Character character) =>
            {
                character.hp -= attack[0] * GetDamage / 100;
                character.hp = math.max(0, character.hp);
            })
            .Schedule();
        //删除NativeArray
        attack.Dispose(Dependency);
        ecbSystem.AddJobHandleForProducer(Dependency);
    }
}