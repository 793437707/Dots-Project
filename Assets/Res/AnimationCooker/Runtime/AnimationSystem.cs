// If this system is enabled, it will animate the vertexes of any entity that has AnimationStateData, AnimationCmdData, and the time and speed properties.
// NOTE (TODO) - currently it only accounts for the per-instance speed value. If you set the _MatSpeed parameter in the shader, bad things will happen
// TODO - I need to figure out how to fetch _MatSpeed efficiently (unfortunately, it's in a shared mesh renderer and to access it would require putting the loop on the main thread)
//
// To change an animation at runtime, set the values in AnimationCmdData to get your desired animation.
//----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//

using Unity.Entities; // for SystemBase
using Unity.Collections.LowLevel.Unsafe;

public partial class AnimationSystem : SystemBase
{
    UnsafeList<UnsafeList<AnimDbEntry>> m_database;

    protected override void OnCreate()
    {
        base.OnCreate();

        // fetch a copy of the database in a native format
        AnimationDatabase.GetDb().GetNativeDatabase(out m_database);
    }

    protected override void OnUpdate()
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        // we must make a local copy of the database here because member variables can't be accessed in a ForEach.
        var localDatabase = m_database;

        // #################### METHOD 3: SENDS TIME TO THE SHADER #######################
        Entities.ForEach((Entity entity, ref MaterialClipIndex clipIndexProp, ref MaterialCurrentTime curTimeProp, ref AnimationStateData state, ref AnimationCmdData cmd, ref MaterialAnimationSpeed speed) => {
            if (cmd.cmd == AnimationCmd.PlayOnce) {
                // we received a play once command, so change the clip and set the state mode.
                clipIndexProp.clipIndex = cmd.clipIndex;
                curTimeProp.time = 0f;
                state.mode = AnimationPlayMode.PlayOnce;
                cmd.cmd = AnimationCmd.None; // indicates command has been processed
                if (cmd.speed > 0f) { speed.multiplier = cmd.speed; }
            } else if (cmd.cmd == AnimationCmd.SetPlayForever) {
                // we received a command to change the play-forever clip, so change state to reflect it
                // and put it into effect if there isn't a play-once command currently executing.
                state.foreverClipIndex = cmd.clipIndex;
                if (state.mode == AnimationPlayMode.PlayForever) {
                    clipIndexProp.clipIndex = state.foreverClipIndex;
                    curTimeProp.time = 0f;
                }
                cmd.cmd = AnimationCmd.None; // indicates command has been processed
            } else if (cmd.cmd == AnimationCmd.PlayOnceAndStop) {
                // we recieved a play-once-and-stop command, so start a play-once-and-stop operation
                state.mode = AnimationPlayMode.PlayOnceAndStop;
                curTimeProp.time = 0f;
                clipIndexProp.clipIndex = cmd.clipIndex;
                cmd.cmd = AnimationCmd.None; // reset (cmd processed)
                if (cmd.speed > 0f) { speed.multiplier = cmd.speed; }
            } else if (cmd.cmd == AnimationCmd.Stop) {
                state.mode = AnimationPlayMode.Stopped;
                cmd.cmd = AnimationCmd.None; // reset (cmd processed)
            } else if (state.mode != AnimationPlayMode.Stopped) {
                // logic here means that no command was sent,
                // so this is where we set the _CurTime property each frame
                AnimDbEntry clip = localDatabase[state.modelIndex][(int)clipIndexProp.clipIndex];
                // end time is (interval * frame count) / speed multipliers
                float endTime = clip.interval * (clip.endFrame - clip.beginFrame + 1) / speed.multiplier;
                //禁用最后0.1s，防止返回到foreverClipIndex重复播放一次导致闪烁，1fps/5FPS都不好使
                if ((curTimeProp.time + deltaTime) >= endTime || state.mode == AnimationPlayMode.PlayOnceAndStop && (curTimeProp.time + 0.1f) >= endTime)
                { // if clip finished playing
                    if (state.mode == AnimationPlayMode.PlayForever) {
                        curTimeProp.time = 0f; // reset to beginning
                    } else if (state.mode == AnimationPlayMode.PlayOnce) {
                        // transition back to forever mode
                        curTimeProp.time = 0f; // show first frame of the forever clip
                        state.mode = AnimationPlayMode.PlayForever;
                        clipIndexProp.clipIndex = state.foreverClipIndex;
                        state.lastPlayedClipIndex = state.currentClipIndex;
                    } else if (state.mode == AnimationPlayMode.PlayOnceAndStop) {
                        state.mode = AnimationPlayMode.Stopped;
                        curTimeProp.time = endTime - clip.interval;
                        state.lastPlayedClipIndex = state.currentClipIndex;
                    }
                }
                if (state.mode != AnimationPlayMode.Stopped) {
                    curTimeProp.time += deltaTime;
                }
            }
            state.currentClipIndex = (byte)clipIndexProp.clipIndex;
        }).ScheduleParallel();
    }
}