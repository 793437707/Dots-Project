using System;
using System.Collections;
using Unity.Collections;
using Unity.Entities;
using Unity.Scenes;
using UnityEngine;

class GameManager : MonoBehaviour
{
    [Tooltip("The target frame-rate for the app. If set it to zero, the rate won't be set.  (default 0)")] public int m_appTargetFrameRate = 0;
    [Tooltip("The vsync count for the app. If set to zero, the count won't be set. (default 0)")] public int m_vSyncCount = 0;

    public static bool MouseAutoSpawn;
    public static int MapSeed = 1919191;

    public SubScene subScene;
    private Entity subSceneEntity;

    private static EntityManager entityManager;
    private static EntityQuery tagQuery;

    public static MapManager mapManager;
    public static CameraManager cameraManager;

    private void Awake()
    {
        mapManager = GetComponent<MapManager>();

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

    public void LoadGameScene()
    {
        Debug.Log("Load Game Start");
        subSceneEntity = SceneSystem.LoadSceneAsync(World.DefaultGameObjectInjectionWorld.Unmanaged, subScene.SceneGUID, new SceneSystem.LoadParameters { Flags = SceneLoadFlags.BlockOnStreamIn});
        
        MapManager.MapSeed = 1919191;
        StartCoroutine(LoadGame());
    }
    IEnumerator LoadGame()
    {
        //等待场景加载完
        yield return null;
        yield return null;
        yield return null;
        mapManager.CreateMap();

        Debug.Log("Load Game End");
    }

    public void UnloadGameScene()
    {
        Debug.Log("Unload Game Start");
        SceneSystem.UnloadScene(World.DefaultGameObjectInjectionWorld.Unmanaged, subSceneEntity);
        Debug.Log("Unload Game End");

    }

    public static void TimeLog(string str = "")
    {
        Debug.Log(string.Format("[{0}] Call [{1}]", DateTime.Now.Ticks.ToString(), str));
    }
}