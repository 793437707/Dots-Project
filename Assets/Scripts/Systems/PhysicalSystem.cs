using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics;
using Unity.Physics.Systems;
using System.Diagnostics;

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
                enemy.hp -= bulletData[a].damage;
                enemyData[b] = enemy;
            }
        }
    }

    protected override void OnUpdate()
    {
        Dependency = new BulletTriggerEvents
        {
            autoDestoryData = GetComponentLookup<AutoDestory>(),
            bulletData = GetComponentLookup<Bullet>(),
            enemyData = GetComponentLookup<Enemy>(),
            localTransformData = GetComponentLookup<LocalTransform>()
        }
        .Schedule(SystemAPI.GetSingleton<SimulationSingleton>(),Dependency);
    }
}