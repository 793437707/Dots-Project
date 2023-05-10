using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

[BurstCompile]
[UpdateInGroup(typeof(InitializationSystemGroup))]
partial class SpawnEnemySystem : SystemBase
{
    private EntityCommandBufferSystem ecbSystem;
    private const float maxSize = 70;
    private const float minSize = 30;
    private static float spawnCD = 0.2f;
    private static float spawnTimer = 3f;
    private static Random random;
    protected override void OnCreate()
    {
        ecbSystem = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
        random = new Random(MapManager.MapSeed);
        spawnCD = 1f;
        spawnTimer = 3f;
    }

    protected override void OnUpdate()
    {
        var ecb = ecbSystem.CreateCommandBuffer().AsParallelWriter();
        float deltaTime = SystemAPI.Time.DeltaTime;
        Entity EnemyZombie = GameManager.GetEntityForTag("EnemyZombie");
        if(EnemyZombie == Entity.Null)
            return;
        Entity Parent = GameManager.GetEntityForTag("Enemy");


        Entities
            .ForEach((in Character character, in LocalToWorld transform, in int entityInQueryIndex) =>
            {
                spawnTimer -= deltaTime * 1;
                if (spawnTimer > 0)
                    return;
                spawnTimer += spawnCD;

                LocalTransform zombieTransform = SystemAPI.GetComponent<LocalTransform>(EnemyZombie);
                Enemy zombieEnemy = SystemAPI.GetComponent<Enemy>(EnemyZombie);

                zombieTransform.Position = transform.Position;
                zombieTransform.Position.y = 0;
                float dis = random.NextFloat() * (maxSize - minSize) + minSize;
                float angel = random.NextFloat() * math.PI * 2;
                zombieTransform.Position.x += dis * math.sin(angel);
                zombieTransform.Position.z += dis * math.cos(angel);

                zombieEnemy.hp *= 1;
                zombieEnemy.damage *= 1;

                //生成实体
                Entity enemy = ecb.Instantiate(entityInQueryIndex, EnemyZombie);
                
                ecb.AddComponent(entityInQueryIndex, enemy, new Parent { Value = Parent });
                ecb.SetComponent(entityInQueryIndex, enemy, zombieTransform);
                ecb.SetComponent(entityInQueryIndex, enemy, zombieEnemy);

            })
            .WithoutBurst()
            .Schedule();



        ecbSystem.AddJobHandleForProducer(Dependency);
    }
}