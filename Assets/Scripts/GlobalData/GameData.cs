using System.Collections;
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
    }

    public int MaxPlayTime;
    public int PlayTimes;
    public int TotalPlayTime;

    string path = Application.persistentDataPath + "/GameData.dat";
    //读取数据
    public void LoadData()
    {
        if(File.Exists(path)) 
        {
            string data = File.ReadAllText(path);
            JsonUtility.FromJsonOverwrite(data, this);
        }
        Debug.LogError("MaxPlayTime:" + MaxPlayTime);
    }
    //保存数据
    public void SavaData()
    {
        string data = JsonUtility.ToJson(this, true);
        File.WriteAllText(path, data);
    }
}