using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct AchievementCondition
{
    [Header("达成条件类型")]
    public GameDataEnum type;
    [Header("达成条件值")]
    public int value;
}

[Serializable]
public struct AchievementData
{
    [Header("标题，任务名称")]
    public string title;
    [Header("描述")]
    public string des;
    [Header("达成条件")]
    public AchievementCondition condition;
    [Header("奖励")]
    public int reward;
}

[CreateAssetMenu(fileName = "AchievementDataBase", menuName = "GameDataBase/AchievementDataBase")]
public class AchievementDataBase : ScriptableObject
{
    public List<AchievementData> data;
}
