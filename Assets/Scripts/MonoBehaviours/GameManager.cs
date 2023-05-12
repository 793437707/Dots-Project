using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Scenes;
using UnityEngine;

class GameManager : MonoBehaviour
{
    public static bool MouseAutoSpawn { get { return GameData.Inst.Option[1]; } }

    public static int OptionSize = 6;
    public static bool inGame = false;

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

    [HideInInspector]
    public bool GameOver = false;
    
    private bool isPause = false;


    private void Awake()
    {
        gameManager = this;

        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        tagQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<Tag>());
        prefabsQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<Prefabs>());
        EntityForTagDictionary = new Dictionary<FixedString64Bytes, Entity>();
    }

    private void Start()
    {
        GameData.Inst.LoadData();
        LoadOption();
    }

    private void LoadOption()
    {
        StartCoroutine(LoadOptionMuteAudio());
    }

    IEnumerator LoadOptionMuteAudio()
    {
        AudioListener.volume = 0;
        uIManager.LoadOption();
        //��ֹ��ȡʱ�����������Ⱦ���һ��
        yield return new WaitForSeconds(0.5f);
        AudioListener.volume = 1;
    }

    public static Entity GetEntityForTag(string name = "Root")
    {
        //�����д���
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
        //��������
        if (GameData.Inst.Option[2])
        {
            var random = new Unity.Mathematics.Random(GameData.Inst.MapSeed);
            uint newSeed = random.NextUInt();
            GameData.Inst.MapSeed = newSeed;
            GameData.Inst.SavaData();
        }


        GameOver = false;
        WorldData.Inst.totalSeconds = 0;
        WorldData.Inst.totalScore = 0;
        CharacterData.Inst.Reset();

        SwitchPause();

        //�����س���
        uIManager.SetLoadingText("���ص�ͼ��...");
        subSceneEntity = SceneSystem.LoadSceneAsync(World.DefaultGameObjectInjectionWorld.Unmanaged, subScene.SceneGUID);
        //�ȴ�����������
        yield return new WaitUntil(() => SceneSystem.IsSceneLoaded(World.DefaultGameObjectInjectionWorld.Unmanaged,subSceneEntity));
        yield return null;

        yield return null;
        InitEntityForTagDictionary();
        yield return null;

        //���ɶ����ͼ
        uIManager.SetLoadingText("���ɵ�ͼ��...");
        mapManager.CreateMap();


        SwitchPause();
        yield return null;
        uIManager.LoadingToGame();
        inGame = true;
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
        uIManager.SetLoadingText("������Ϸ��...");
        EntityForTagDictionary.Clear();
        SceneSystem.UnloadScene(World.DefaultGameObjectInjectionWorld.Unmanaged, subSceneEntity);
        yield return new WaitForSecondsRealtime(0.3f);
        yield return null;

        SwitchPause();
        inGame = false;
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
        SaveLocalGameData();
        uIManager.ShowDead();
    }

    public void SaveLocalGameData()
    {
        //�޸ļ�¼���
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