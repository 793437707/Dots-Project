// This class will query for bake-animated entities and change thier animations at a specified interval.
// This class will affect ALL bake-animated entities in the scene (entities that have AnimationStateData). 
// If you have more than one of these classes in the scene at a time, they may conflict (depending on their intervals)
//--------------------------------------------------------------------------------------------------//

using UnityEngine;
using Unity.Entities;
using Unity.Collections;

public class AnimationChanger : MonoBehaviour
{
    [Tooltip("Amount of time in seconds to wait before changing the animation (default 5)")] public float m_interval = 5f;
    [Tooltip("True to enable this script, and false to disable it (default false)")] public bool m_enable = false;

    [TextArea]
    public readonly string msg1 = "**This script will cause any entities with animations to change clips at the specified interval.";
    [TextArea]
    public readonly string msg2 = "**Each clip will run once, and then go back to the 'forever' animation.";

    EntityManager m_em;
    EntityQuery m_query;
    float m_currentTime = 0f;

    AnimationDatabase m_db;

    // Start is called before the first frame update
    void Start()
    {
        m_em = World.DefaultGameObjectInjectionWorld.EntityManager;
        m_query = m_em.CreateEntityQuery(ComponentType.ReadOnly<AnimationStateData>(), ComponentType.ReadOnly<AnimationCmdData>());
        m_db = AnimationDatabase.GetDb();
    }

    // Update is called once per frame
    void Update()
    {
        if (!m_enable) { return; }
        m_currentTime += Time.deltaTime;
        if (m_currentTime < m_interval) { return; }

        NativeArray<AnimationStateData> states = m_query.ToComponentDataArray<AnimationStateData>(Allocator.Temp);
        NativeArray<Entity> entities = m_query.ToEntityArray(Allocator.TempJob);
        for (int i = 0; i < entities.Length; i++) {
            AnimationStateData state = states[i];
            int clipIndex = state.lastPlayedClipIndex + 1; // the next clip
            int clipCount = m_db.GetClipCount(state.modelIndex); // total clip count for this model
            if (clipIndex >= clipCount) { clipIndex = 0; } // wrap (ensure next clip is in range)
            m_em.SetComponentData<AnimationCmdData>(entities[i], new AnimationCmdData() { clipIndex = (byte)clipIndex, cmd = AnimationCmd.PlayOnce });
        }

        entities.Dispose();
        states.Dispose();

        m_currentTime = 0; // reset the timer
    }
}
