using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Scenes;
using Unity.Transforms;
using UnityEngine;

class GameManager : MonoBehaviour
{
    [Tooltip("The target frame-rate for the app. If set it to zero, the rate won't be set.  (default 0)")] public int m_appTargetFrameRate = 0;
    [Tooltip("The vsync count for the app. If set to zero, the count won't be set. (default 0)")] public int m_vSyncCount = 0;

    public static bool MouseAutoSpawn;
    public static int MapSeed = 1919191;
    public SubScene scene;

    private static EntityManager entityManager;
    private static EntityQuery tagQuery;

    private void Awake()
    {
        if (m_appTargetFrameRate >= 0) { Application.targetFrameRate = m_appTargetFrameRate; }
        if (m_vSyncCount >= 0) { QualitySettings.vSyncCount = m_vSyncCount; }
        LoadSetting();

        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        tagQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<Tag>());
    }

    private void LoadSetting()
    {
        MouseAutoSpawn = true;
    }

    public static Entity GetEntityForTag(string name = "Root")
    {
        var entities = tagQuery.ToEntityArray(Allocator.TempJob);
        Entity entity = Entity.Null;
        for(int i = 0; i < entities.Length; i ++)
        {
            var tag = entityManager.GetComponentData<Tag>(entities[i]);
            if (tag.tag == name)
                entity = entities[i];
        }
        entities.Dispose();
        return entity;
    }


}