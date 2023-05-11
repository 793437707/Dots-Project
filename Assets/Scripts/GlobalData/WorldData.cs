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
}