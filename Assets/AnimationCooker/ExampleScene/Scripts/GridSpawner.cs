// GridSpawner - A class that can spawn any number of entities in a grid. It's mainly used for testing purposes.
//
// You typically will need a Text field, an Input field, and a Button in your GUI and then attach those to this class,
// which allows the player to spawn things at runtime.
//
// This class will spawn any entities it finds that have a AnimationStateData component attached to them.
// As entities are spawned, they will be setup to play different animation clips.
//--------------------------------------------------------------------------------------------------//

using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;
using TMPro;

// any entity that is spawned will be given this tag
// (it's used for deleting spawns)
//public struct SpawnedTag : IComponentData { }

public class GridSpawner : MonoBehaviour
{
    [Tooltip("Spacing between entities (default 1.5)")] public float m_spacing = 10f;
    [Tooltip("A text object that will get updated with the current spawn count (optional)")] public TextMeshProUGUI m_statusText = null;
    [Tooltip("An input field object that lets the player enter the spawn count")] public TMP_InputField m_inputField = null;
    [Tooltip("If set to true, spawns will be tinted based on the animation clip they are playing.")] public bool m_enableVaryColorsByAnimation = false;
    [Tooltip("If set to true, each instance's animation speed will vary randomly.")] public bool m_enableVarySpeed = false;
    [Tooltip("If set to true, alternate animation clips for each spawn.")] public bool m_enableVaryClip = false;

    [TextArea]
    public string m_info = "This spawner will spawn any items placed in a subscene.";

    EntityManager m_em;
    float3 m_pos;
    float m_width = 0f;
    int m_spawnCount = 0;
    AnimationDatabase m_db;

    Color[] m_colors = new Color[] { Color.red, Color.green, Color.blue, Color.cyan, Color.white, Color.grey, Color.magenta, Color.yellow };

    // Start is called before the first frame update
    void Start()
    {
        m_db = AnimationDatabase.GetDb();
        m_em = World.DefaultGameObjectInjectionWorld.EntityManager;
        UpdateCountText();
    }

    // deletes any entity that has a SpawnedTag
    public void ClearSpawns()
    {
        EntityQuery query = m_em.CreateEntityQuery(ComponentType.ReadOnly<SpawnedTag>());
        if (query.CalculateEntityCount() > 0) { m_em.DestroyEntity(query); }
        UpdateCountText();
    }

    public void BatchSpawn()
    {
        ClearSpawns();

        // this will only pick up prefabs because all the spawned entities were cleared by ClearSpawns()
        EntityQuery query = m_em.CreateEntityQuery(ComponentType.ReadOnly<AnimationStateData>());
        NativeArray<Entity> prefabs = query.ToEntityArray(Allocator.Temp);

        if (prefabs.Length <= 0) {
            m_statusText.text = "No entities found with AnimationStateData. Add a baked animation prefab to the subscene.";
            UnityEngine.Debug.Log("No entities found with AnimationStateData. Add a baked animation prefab to the subscene.");
            return;
        }

        int count = 0;
        m_pos = transform.position;
        m_spawnCount = int.Parse(m_inputField.text);
        m_width = math.sqrt(m_spawnCount) * m_spacing;
        m_pos.x -= m_width * 0.5f;
        m_pos.z -= m_width * 0.5f;
        int wholePart = m_spawnCount / prefabs.Length;
        int remainder = m_spawnCount - (wholePart * prefabs.Length);
        // for every prefab, spawn wholePart instances
        for (int i = 0; i < prefabs.Length; i++) {
            // if we're on the last index, include the remainder
            if (i == prefabs.Length - 1) { wholePart += remainder; }

            // do a batch instantiation for this row
            NativeArray<Entity> entities = m_em.Instantiate(prefabs[i], wholePart, Allocator.Temp);

            // set the position for each of the new entities in this row
            // also change color, animation clip, and speed based on user options
            for (int j = 0; j < entities.Length; j++) {
                Entity entity = entities[j];
                int modelIndex = m_em.GetComponentData<AnimationStateData>(entity).modelIndex;

                // this will cycle through clip indexes (optional depending on which variances are used)
                int clipIdx = j % m_db.GetClipCount(modelIndex);

                // move this entity to its location in the grid

                LocalTransform xForm = m_em.GetComponentData<LocalTransform>(entity);
                xForm.Position = m_pos;
                m_em.SetComponentData<LocalTransform>(entity, xForm);

                if (m_enableVaryClip) {
                    // set the animation for this clip
                    m_em.SetComponentData(entity, new AnimationCmdData() { clipIndex = (byte)clipIdx, cmd = AnimationCmd.PlayOnce });
                }

                // set the color based on the clip index.
                if (m_enableVaryColorsByAnimation) {
                    UnityColorToFloat4(m_colors[Mathf.Clamp(clipIdx, 0, m_colors.Length)], out float4 f4color);
                    m_em.AddComponentData(entity, new Unity.Rendering.URPMaterialPropertyBaseColor() { Value = f4color });
                }

                // randomly vary the animation speed
                if (m_enableVarySpeed) {
                    m_em.SetComponentData(entity, new MaterialAnimationSpeed { multiplier = UnityEngine.Random.Range(0.5f, 2f) });
                }

                m_em.AddComponent<SpawnedTag>(entity);
                IncrementPosition(ref m_pos);
            }
            count += entities.Length;
            entities.Dispose();
        }
        UpdateCountText();
    }

    // refreshes the total number of entities spawned
    void UpdateCountText()
    {
        if (m_statusText == null) { return; }
        EntityQuery query = m_em.CreateEntityQuery(ComponentType.ReadOnly<AnimationStateData>());
        m_statusText.text = $"{query.CalculateEntityCount()} of {m_spawnCount}";
    }

    // increments the position to the next slot such that the spawner is in the middle of the spawn area
    void IncrementPosition(ref float3 pos)
    {
        // increment position
        pos.x += m_spacing;
        if (pos.x > ((m_width * 0.5f) + transform.position.x)) {
            pos.x = transform.position.x - (m_width * 0.5f);
            pos.z += m_spacing;
        }
    }

    // convert a unity color to a float4
    static void UnityColorToFloat4(in Color color, out float4 f4)
    {
        f4.x = color.r;
        f4.y = color.g;
        f4.z = color.b;
        f4.w = color.a;
    }
}
