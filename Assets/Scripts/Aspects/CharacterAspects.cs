using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Unity.Physics;

readonly partial struct CharacterAspects:IAspect
{
    public readonly RefRW<Character> character;
    public readonly RefRW<LocalTransform> transform;
    public readonly RefRW<PhysicsVelocity> velocity;
}