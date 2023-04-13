using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

readonly partial struct SpawnMobAspects : IAspect
{
    public readonly RefRW<SpawnMobs> spawnMobs;
    public readonly RefRO<LocalToWorld> spawnTransform;
}