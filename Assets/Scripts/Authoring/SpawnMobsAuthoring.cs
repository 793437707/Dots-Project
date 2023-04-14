using Unity.Entities;
using UnityEngine;

class SpawnMobsAuthoring : MonoBehaviour
{
    public bool autoSpawn = true;
    public bool mouseSpawn = false;
    public GameObject spawnPrefab = null;
    public float spawnCD = 1f;
    public float nextSpawnTime = 1f;
    public bool autoMove = true;
    public float moveSpeed = 10f;
}

class SpawnMobsBaker : Baker<SpawnMobsAuthoring>
{
    public override void Bake(SpawnMobsAuthoring authoring)
    {
        var e = GetEntity(authoring, TransformUsageFlags.Dynamic);
        AddComponent(e, new SpawnMobs
        {
            autoSpawn = authoring.autoSpawn,
            mouseSpawn = authoring.mouseSpawn,
            spawnPrefab = GetEntity(authoring.spawnPrefab, TransformUsageFlags.Dynamic),
            spawnCD = authoring.spawnCD,
            nextSpawnTime = authoring.nextSpawnTime,
            autoMove = authoring.autoMove,
            moveSpeed = authoring.moveSpeed
        });
    }
}