using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[BurstCompile]
[UpdateInGroup(typeof(PresentationSystemGroup))]
partial class AnimationChangerSystem : SystemBase
{
    protected override void OnUpdate()
    {
        Entities
            .ForEach((ref CharacterAspects character, ref AnimationCmdData cmd, in AnimationStateData state) =>
            {
                AnimDb.Fox newIndex = character.velocity.ValueRO.Linear.Equals(float3.zero) ? AnimDb.Fox.Fox_Idle : AnimDb.Fox.Fox_Run_InPlace;
                if (state.foreverClipIndex != (byte)newIndex)
                {
                    cmd.cmd = AnimationCmd.SetPlayForever;
                    cmd.clipIndex = (byte)newIndex;
                }
            })
            .Schedule();
    }

}