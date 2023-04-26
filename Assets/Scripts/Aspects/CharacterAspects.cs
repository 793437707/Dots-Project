using Unity.Entities;
using Unity.Transforms;
using Unity.Physics;

readonly partial struct CharacterAspects:IAspect
{
    public readonly RefRW<Character> character;
    public readonly RefRW<LocalTransform> localTransform;
    public readonly RefRW<PhysicsVelocity> velocity;
}