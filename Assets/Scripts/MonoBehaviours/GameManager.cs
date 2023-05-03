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
    public static UIManager uIManager;
    public static GameManager gameManager;
    public static DatabasesManager databasesManager;

    public bool GameOver = false;

    private bool isPause = false;

    private void Awake()
    {
        gameManager = this;
        if (m_appTargetFrameRate >= 0) { Application.targetFrameRate = m_appTargetFrameRate; }
        if (m_vSyncCount >= 0) { QualitySettings.vSyncCount = m_vSyncCount; }

        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        tagQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<Tag>());
    }

    private void Start()
    {
        GameData.Inst.LoadData();
        LoadSetting();
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
        StartCoroutine(LoadGame());
    }
    IEnumerator LoadGame()
    {
        //加载数据
        MapManager.MapSeed = 1919191;
        GameOver = false;
        WorldData.Inst.totalSeconds = 0;
        CharacterData.Inst.Reset();

        SwitchPause();

        //最后加载场景
        subSceneEntity = SceneSystem.LoadSceneAsync(World.DefaultGameObjectInjectionWorld.Unmanaged, subScene.SceneGUID);
        //等待场景加载完
        yield return new WaitUntil(() => SceneSystem.IsSceneLoaded(World.DefaultGameObjectInjectionWorld.Unmanaged,subSceneEntity));
        yield return null;
        //生成额外地图
        mapManager.CreateMap();


        SwitchPause();
        Debug.Log("Load Game End");
    }

    public void UnloadGameScene()
    {
        Debug.Log("Unload Game Start");
        SwitchPause();
        SceneSystem.UnloadScene(World.DefaultGameObjectInjectionWorld.Unmanaged, subSceneEntity);
        SwitchPause();
        Debug.Log("Unload Game End");

    }
    public void SwitchPause()
    {
        isPause = !isPause;
        if (isPause)
        {
            Time.timeScale = 0;
            Debug.Log("Game Paused!");
        }
        else
        {
            Time.timeScale = 1;
            Debug.Log("Game Resumed!");
        }
    }

    public void GameDead()
    {
        SwitchPause();
        GameOver = true;
        uIManager.ShowDead();
        SaveLocalGameData();
    }

    public void SaveLocalGameData()
    {
        //修改记录相关
        GameData.Inst.MaxPlayTime = Math.Max(GameData.Inst.MaxPlayTime, (int)WorldData.Inst.totalSeconds);
        GameData.Inst.PlayTimes += 1;
        GameData.Inst.TotalPlayTime += (int)WorldData.Inst.totalSeconds;

        GameData.Inst.SavaData();
    }

    public static void TimeLog(string str = "")
    {
        Debug.Log(string.Format("[{0}] Call [{1}]", DateTime.Now.Ticks.ToString(), str));
    }
}