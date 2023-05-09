using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class GameData
{
    //单例模式
    private static GameData inst;
    public static GameData Inst
    {
        get
        {
            if (inst == null)
            {
                inst = new GameData();
            }
            return inst;
        }
    }
    private GameData()
    {
        MaxPlayTime = 0;
        PlayTimes = 0;
        TotalPlayTime = 0;
        GlodCoin = 0;
        AchievementReceive = new List<bool>();
        TianFuLevel = new List<int>();
    }

    public int MaxPlayTime;
    public int PlayTimes;
    public int TotalPlayTime;
    public int GlodCoin;

    public List<bool> AchievementReceive;
    public List<int> TianFuLevel;

    string path = Application.persistentDataPath + "/GameData.dat";
    //读取数据
    public void LoadData()
    {
        if (File.Exists(path))
        {
            string data = File.ReadAllText(path);
            JsonUtility.FromJsonOverwrite(data, this);
        }
        //读取databases，保证数组长度一致性
        var achiecementSize = GameManager.databasesManager.AchievementGetSize();
        while(AchievementReceive.Count < achiecementSize)
            AchievementReceive.Add(false);
        var tianfuSize = GameManager.databasesManager.TianFuGetSize();
        while(TianFuLevel.Count < tianfuSize)
            TianFuLevel.Add(0);

    }
    //保存数据
    public void SavaData()
    {
        string data = JsonUtility.ToJson(this, true);
        File.WriteAllText(path, data);
    }

    public int GetValueByEnum(GameDataEnum name)
    {
        return (int)GetType().GetField(name.ToString()).GetValue(this);
    }

    public void AddValueByEnum(GameDataEnum name, int value)
    {
        GetType().GetField(name.ToString()).SetValue(this, GetValueByEnum(name) + value);
        SavaData();
    }
}

public enum GameDataEnum
{
    MaxPlayTime,
    PlayTimes,
    TotalPlayTime,
    GlodCoin,
}