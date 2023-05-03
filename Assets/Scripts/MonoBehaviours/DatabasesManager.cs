using System.Collections;
using Unity.Mathematics;
using UnityEngine;


public class DatabasesManager : MonoBehaviour
{
    private void Awake()
    {
        GameManager.databasesManager = this;
    }
    public AchievementDataBase achievement;
    public TianFuDataBase tianfu;


    public int AchievementGetSize()
    {
        return achievement.data.Count;
    }

    public bool AchievementIsFinished(int id)
    {
        return GameData.Inst.AchievementReceive[id];
    }

    public int2 AchievementProgress(int id)
    {
        var condition = achievement.data[id].condition;
        int2 progress = new int2(GameData.Inst.GetValueByEnum(condition.type), condition.value);
        progress.x = math.min(progress.x, progress.y);
        return progress;
    }

    public void AchievementFinish(int id)
    {
        if(AchievementIsFinished(id))
        {
            Debug.LogError("Achievement is already finished!");
            return;
        }
        GameData.Inst.AchievementReceive[id] = true;
        GameData.Inst.GlodCoin += achievement.data[id].reward;
        GameData.Inst.SavaData();
        Debug.Log("Finish Achievement! id = " + id);
    }




    public int TianFuGetSize()
    {
        return tianfu.data.Count;
    }

    public int TianFuGetLevel(int id)
    {
        return GameData.Inst.TianFuLevel[id];
    }

    public int TianFuGetCost(int id, int level)
    {
        return tianfu.data[id].cost[level];
    }

    public int TianFuGetMaxLevel(int id)
    {
        return tianfu.data[id].cost.Length;
    }

    public Sprite TianFuGetIcon(int id)
    {
        return tianfu.images[tianfu.data[id].iconID];
    }

    public void TianfuAddLevel(int id, int num)
    {
        if (TianFuGetLevel(id) + num < 0 || TianFuGetLevel(id) + num > TianFuGetMaxLevel(id))
        {
            Debug.LogError("TianFu level is out of range! id = " + id);
            return;
        }
        if(num == -1)
        {
            GameData.Inst.TianFuLevel[id]--;
            GameData.Inst.GlodCoin += TianFuGetCost(id, TianFuGetLevel(id));
        }
        else if(num == 1)
        {
            GameData.Inst.GlodCoin -= TianFuGetCost(id, TianFuGetLevel(id));
            GameData.Inst.TianFuLevel[id]++;
        }
        else
        {
            Debug.LogError("TianFu Add Level Not 1 or -1! id = " + id);
            return;
        }
        GameData.Inst.SavaData();
    }
}