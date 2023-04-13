using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(SimulationSystemGroup))] //Update
//[UpdateInGroup(typeof(PresentationSystemGroup))] //LateUpdate
//[UpdateBefore(typeof(CharacterSystem))]
//[UpdateAfter(typeof(CharacterSystem))]
partial class CharacterSystem : SystemBase
{
    protected override void OnUpdate()
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        const float rotateSpeed = 10f;

        Vector3 input = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized;

        Quaternion quaInput = input == Vector3.zero ? Quaternion.identity : Quaternion.LookRotation(input, Vector3.up);

        Entities
            .ForEach((CharacterAspects character) =>
            {
                character.transform.ValueRW.Rotation = Quaternion.Lerp(character.transform.ValueRO.Rotation, quaInput, deltaTime * rotateSpeed);
                character.velocity.ValueRW.Linear = input * character.character.ValueRO.moveSpeed * 0.02f;
            })
            .ScheduleParallel();
    }
    
}

