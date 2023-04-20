using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

[BurstCompile]
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
        bool mouseInput = Input.GetMouseButton(0) || GameManager.MouseAutoSpawn;
        //鼠标输入的射线
        UnityEngine.Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        Entities.
            ForEach((ref SpawnMobAspects spawnMobAspects, in int entityInQueryIndex) =>
            {
                spawnMobAspects.spawnMobs.ValueRW.nextSpawnTime -= deltaTime;
                if ((spawnMobAspects.spawnMobs.ValueRO.autoSpawn || spawnMobAspects.spawnMobs.ValueRO.mouseSpawn && mouseInput)
                    && spawnMobAspects.spawnMobs.ValueRO.nextSpawnTime < 0)
                {
                    //设置CD
                    spawnMobAspects.spawnMobs.ValueRW.nextSpawnTime = spawnMobAspects.spawnMobs.ValueRO.spawnCD;
                    //生成物体
                    var instance = ecb.Instantiate(entityInQueryIndex, spawnMobAspects.spawnMobs.ValueRO.spawnPrefab);
    
                    LocalTransform localTransform = LocalTransform.FromPositionRotation(
                        spawnMobAspects.spawnTransform.ValueRO.Position, spawnMobAspects.spawnTransform.ValueRO.Rotation);

                    //鼠标输入时修改方向
                    if (spawnMobAspects.spawnMobs.ValueRO.mouseSpawn && mouseInput)
                    {
                        //与狐狸Y轴平面相交
                        UnityEngine.Plane plane = new UnityEngine.Plane(Vector3.up, localTransform.Position);
                        if (plane.Raycast(ray, out float distance))
                        {
                            float3 hitPoint = ray.GetPoint(distance);
                            float3 direction = math.normalize(hitPoint - localTransform.Position);
                            localTransform.Rotation = Quaternion.LookRotation(direction);
                        }
                    }

                    //设置物体transform
                    ecb.SetComponent(entityInQueryIndex, instance, localTransform);

                    //自动飞行的物体给速度
                    if(spawnMobAspects.spawnMobs.ValueRO.autoMove)
                    {
                        ecb.SetComponent(entityInQueryIndex, instance, new PhysicsVelocity
                        {
                            Linear = localTransform.Forward() * spawnMobAspects.spawnMobs.ValueRO.moveSpeed
                        }) ;
                    }
                }
            })
            .Schedule();

        ecbSystem.AddJobHandleForProducer(Dependency);
    }
}
