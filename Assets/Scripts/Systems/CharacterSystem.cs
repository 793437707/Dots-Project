using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

partial class CharacterSystem : SystemBase
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<CharacterAspects>();
    }
    protected override void OnUpdate()
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        const float rotateSpeed = 10f;

        Vector3 input = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized;

        Quaternion quaInput = Quaternion.LookRotation(input, Vector3.up);

        Entities
            .ForEach((CharacterAspects character) =>
            {
                character.transform.ValueRW.Rotation = Quaternion.Lerp(character.transform.ValueRO.Rotation, quaInput, deltaTime * rotateSpeed);
                character.velocity.ValueRW.Linear = input * character.character.ValueRO.moveSpeed * deltaTime;
            })
            .ScheduleParallel();
    }
}

