using System;
using System.Collections;
using System.Collections.Generic;
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
    private static EntityQuery prefabsQuery;

    public static MapManager mapManager;
    public static CameraManager cameraManager;
    public static UIManager uIManager;
    public static GameManager gameManager;
    public static DatabasesManager databasesManager;

    public static Dictionary<FixedString64Bytes, Entity> EntityForTagDictionary;

    public bool GameOver = false;

    private bool isPause = false;

    private void Awake()
    {
        gameManager = this;
        if (m_appTargetFrameRate >= 0) { Application.targetFrameRate = m_appTargetFrameRate; }
        if (m_vSyncCount >= 0) { QualitySettings.vSyncCount = m_vSyncCount; }

        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        tagQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<Tag>());
        prefabsQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<Prefabs>());
        EntityForTagDictionary = new Dictionary<FixedString64Bytes, Entity>();
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
        //缓存中存在
        if(EntityForTagDictionary.ContainsKey(name))
            return EntityForTagDictionary[name];
        return Entity.Null;
    }

    public void InitEntityForTagDictionary()
    {
        var entities = tagQuery.ToEntityArray(Allocator.TempJob);
        for (int i = 0; i < entities.Length; i++)
        {
            var tag = entityManager.GetComponentData<Tag>(entities[i]);
            EntityForTagDictionary.Add(tag.tag, entities[i]);
            entityManager.RemoveComponent<Tag>(entities[i]);
        }
        entities.Dispose();
        entities = prefabsQuery.ToEntityArray(Allocator.TempJob);
        for (int i = 0; i < entities.Length; i++)
        {
            var prefabs = entityManager.GetComponentData<Prefabs>(entities[i]);
            EntityForTagDictionary.Add(prefabs.name, prefabs.Entity);
            entityManager.RemoveComponent<Prefabs>(entities[i]);
        }
        entities.Dispose();
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
        WorldData.Inst.totalScore = 0;
        CharacterData.Inst.Reset();

        SwitchPause();

        //最后加载场景
        uIManager.SetLoadingText("加载地图中...");
        subSceneEntity = SceneSystem.LoadSceneAsync(World.DefaultGameObjectInjectionWorld.Unmanaged, subScene.SceneGUID);
        //等待场景加载完
        yield return new WaitUntil(() => SceneSystem.IsSceneLoaded(World.DefaultGameObjectInjectionWorld.Unmanaged,subSceneEntity));
        yield return null;

        yield return null;
        InitEntityForTagDictionary();
        yield return null;

        //生成额外地图
        uIManager.SetLoadingText("生成地图中...");
        mapManager.CreateMap();


        SwitchPause();
        yield return null;
        uIManager.LoadingToGame();
        Debug.Log("Load Game End");
    }

    public void UnloadGameScene()
    {
        Debug.Log("Unload Game Start");
        StartCoroutine(UnloadGame());
    }

    IEnumerator UnloadGame()
    {
        SwitchPause();
        uIManager.SetLoadingText("保存游戏中...");
        EntityForTagDictionary.Clear();
        SceneSystem.UnloadScene(World.DefaultGameObjectInjectionWorld.Unmanaged, subSceneEntity);
        yield return new WaitForSecondsRealtime(0.3f);
        yield return null;

        SwitchPause();
        uIManager.LoadingToMain();
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
        GameData.Inst.MaxScore = Math.Max(GameData.Inst.MaxScore, WorldData.Inst.totalScore);
        GameData.Inst.TotalScore += WorldData.Inst.totalScore;

        GameData.Inst.SavaData();
    }

    public static void TimeLog(string str = "")
    {
        Debug.Log(string.Format("[{0}] Call [{1}]", DateTime.Now.Ticks.ToString(), str));
    }
}