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
    [Header("ID，用于存储数据，等同下标")]
    public int id;
    [Header("描述")]
    public string des;
    [Header("达成条件")]
    public AchievementCondition condition;
    [Header("前置任务ID")]
    public int preId;
    [Header("奖励")]
    public int reward;
}

[CreateAssetMenu(fileName = "AchievementDataBase", menuName = "GameDataBase/AchievementDataBase")]
public class AchievementDataBase : ScriptableObject
{
    public List<AchievementData> data;
}
