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
}