using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

struct SpawnMobs : IComponentData
{
    public bool autoSpawn;
    public bool mouseSpawn;
    public Entity spawnPrefab;
    public float spawnCD;
    public float nextSpawnTime;
}