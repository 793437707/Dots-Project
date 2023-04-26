using Unity.Entities;
using Unity.Transforms;
using Unity.Physics;

readonly partial struct EnemyAspects : IAspect
{
    public readonly RefRW<Enemy> enemy;
    public readonly RefRW<LocalToWorld> transform;
    public readonly RefRW<LocalTransform> localTransform;
    public readonly RefRW<PhysicsVelocity> velocity;
}