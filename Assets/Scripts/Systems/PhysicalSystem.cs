using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Collections;
using System.Diagnostics;
using System.ComponentModel;
using Unity.Burst;

[BurstCompile]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
//[UpdateAfter(typeof(PhysicsSimulationGroup))] // We are updating after `PhysicsSimulationGroup` - this means that we will get the events of the current frame.
[UpdateAfter(typeof(PhysicsSystemGroup))]
partial class PhysicalSystem : SystemBase
{
    public static float3 DestoryPos = new float3(-10000, -1000, -10000);
    public partial struct BulletTriggerEvents : ITriggerEventsJob
    {
        public ComponentLookup<AutoDestory> autoDestoryData;
        public ComponentLookup<Bullet> bulletData;
        public ComponentLookup<Enemy> enemyData;
        public ComponentLookup<LocalTransform> localTransformData;
        public ComponentLookup<Box> boxData;
        public int Damage;
        [WriteOnly]
        public NativeArray<int> damageOut;
        public void Execute(TriggerEvent collisionEvent)
        {
            Entity a = Entity.Null, b = Entity.Null;
            if (bulletData.HasComponent(collisionEvent.EntityA)) { a = collisionEvent.EntityA; b = collisionEvent.EntityB; }
            else if (bulletData.HasComponent(collisionEvent.EntityB)) { a = collisionEvent.EntityB; b = collisionEvent.EntityA; }
            else return;
            //销毁子弹
            AutoDestory autoDestory = autoDestoryData[a];
            autoDestory.destoryTime = -1;
            autoDestoryData[a] = autoDestory;
            //直接转移掉，防止销毁延迟导致二次碰撞
            LocalTransform localTransform = localTransformData[a];
            localTransform.Position = DestoryPos;
            localTransformData[a] = localTransform;
            //子弹射击敌人
            if (enemyData.HasComponent(b))
            {
                Enemy enemy = enemyData[b];
                enemy.hp -= bulletData[a].damage * Damage / 100;
                enemyData[b] = enemy;
            }
            //子弹射击箱子
            if (boxData.HasComponent(b))
            {
                Box box = boxData[b];
                box.hp -= 1;
                boxData[b] = box;
            }
        }
    }

    protected override void OnUpdate()
    {
        NativeArray<int> damageOut = new NativeArray<int>(1, Allocator.TempJob);

        Dependency = new BulletTriggerEvents
        {
            autoDestoryData = GetComponentLookup<AutoDestory>(),
            bulletData = GetComponentLookup<Bullet>(),
            enemyData = GetComponentLookup<Enemy>(),
            localTransformData = GetComponentLookup<LocalTransform>(),
            boxData = GetComponentLookup<Box>(),
            Damage = CharacterData.Inst.Damage,
            damageOut = damageOut
        }
        .Schedule(SystemAPI.GetSingleton<SimulationSingleton>(),Dependency);

        //吸血
        Entities
            .WithAll<Character>()
            .ForEach(() =>
            {
                CharacterData.Inst.hp += damageOut[0] * CharacterData.Inst.XiXue / 100;
                CharacterData.Inst.hp = math.min(CharacterData.Inst.hpMax, CharacterData.Inst.hp);
            })
            .WithoutBurst()
            .Schedule();

        //删除NativeArray
        damageOut.Dispose(Dependency);
    }
}