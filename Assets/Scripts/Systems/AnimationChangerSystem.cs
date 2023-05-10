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
                float3 speed = character.velocity.ValueRO.Linear;
                speed.y = 0;
                AnimDb.Fox newIndex = speed.Equals(float3.zero) ? AnimDb.Fox.Fox_Idle : AnimDb.Fox.Fox_Run_InPlace;
                if (state.foreverClipIndex != (byte)newIndex)
                {
                    cmd.cmd = AnimationCmd.SetPlayForever;
                    cmd.clipIndex = (byte)newIndex;
                }
            })
            .Schedule();
        Entities
            .ForEach((ref Enemy enemy, ref AnimationCmdData cmd, ref MaterialAnimationSpeed speed, in AnimationStateData state) =>
            {
                switch(enemy.animatior)
                {
                    case EnemyAnimatior.AttackFirst:
                        cmd.cmd = AnimationCmd.PlayOnce;
                        cmd.clipIndex = (byte)AnimDb.Zombie.Z_Attack;
                        speed.multiplier = 1f;
                        break;
                    case EnemyAnimatior.Run:
                        if(state.foreverClipIndex != (byte)AnimDb.Zombie.Z_Run_InPlace)
                        {
                            cmd.cmd = AnimationCmd.SetPlayForever;
                            cmd.clipIndex = (byte)AnimDb.Zombie.Z_Run_InPlace;
                            speed.multiplier = 1f;
                        }
                        break;
                    case EnemyAnimatior.Walk:
                        if (state.foreverClipIndex != (byte)AnimDb.Zombie.Z_Walk_InPlace)
                        {
                            cmd.cmd = AnimationCmd.SetPlayForever;
                            cmd.clipIndex = (byte)AnimDb.Zombie.Z_Walk_InPlace;
                            speed.multiplier = 2.5f;
                        }
                        break;
                    case EnemyAnimatior.Idle:
                        if (state.foreverClipIndex != (byte)AnimDb.Zombie.Z_Idle)
                        {
                            cmd.cmd = AnimationCmd.SetPlayForever;
                            cmd.clipIndex = (byte)AnimDb.Zombie.Z_Idle;
                            speed.multiplier = 2f;
                        }
                        break;
                    case EnemyAnimatior.DeadFirst:
                        enemy.animatior = EnemyAnimatior.Dead;
                        cmd.cmd = AnimationCmd.PlayOnceAndStop;
                        cmd.clipIndex = (byte)AnimDb.Zombie.Z_FallingBack;
                        speed.multiplier = 1f;
                        break;
                }
            })
            .ScheduleParallel();
    }

}