using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))] //Update
//[UpdateInGroup(typeof(PresentationSystemGroup))] //LateUpdate
//[UpdateBefore(typeof(CharacterSystem))]
//[UpdateAfter(typeof(CharacterSystem))]
partial class CharacterSystem : SystemBase
{
    protected override void OnUpdate()
    {
        if (!GameManager.inGame)
            return;
        float deltaTime = SystemAPI.Time.DeltaTime;

        const float rotateSpeed = 10f;

        Vector3 input = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized;

        Quaternion quaInput = input == Vector3.zero ? Quaternion.identity : Quaternion.LookRotation(input, Vector3.up);

        Entities
            .ForEach((ref CharacterAspects character) =>
            {
                if(input != Vector3.zero)
                    character.localTransform.ValueRW.Rotation = Quaternion.Lerp(character.localTransform.ValueRO.Rotation, quaInput, deltaTime * rotateSpeed);
                var gravity = character.velocity.ValueRO.Linear.y;
                character.velocity.ValueRW.Linear = input * character.character.ValueRO.moveSpeed;
                character.velocity.ValueRW.Linear.y = gravity;
            })
            .Schedule();
        //死亡判定，由于死亡后游戏暂停，只会触发一次。
        Entities
            .WithAll<Character>()
            .ForEach(() =>
            {
                if(CharacterData.Inst.hp <= 0 && !GameManager.gameManager.GameOver)
                {
                    GameManager.gameManager.GameDead();
                }
            })
            .WithoutBurst()
            .Run();
    }
    
}

