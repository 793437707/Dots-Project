using System.Collections;
using UnityEngine;

public class WorldData
{
    //单例模式
    private static WorldData inst;
    public static WorldData Inst
    {
        get
        {
            if (inst == null)
            {
                inst = new WorldData();
            }
            return inst;
        }
    }

    private WorldData()
    {
        totalSeconds = 0;
        totalScore = 0;
    }

    public float totalSeconds;
    public int totalScore;
    public int minute => (int)totalSeconds / 60;
    public int second => (int)totalSeconds % 60;

    public float different => Mathf.Min(70, Mathf.Log10(totalSeconds / 6 + 100) * 100 - 200);
    public float differentHpAdd => different * 3 / 100;
    public float differentDmgAdd => different * 2 / 100;
    public float differentSpawnAdd => different / 100;
}