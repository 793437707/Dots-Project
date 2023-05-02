using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct AchievementCondition
{
    [Header("�����������")]
    public GameDataEnum type;
    [Header("�������ֵ")]
    public int value;
}

[Serializable]
public struct AchievementData
{
    [Header("ID�����ڴ洢���ݣ���ͬ�±�")]
    public int id;
    [Header("����")]
    public string des;
    [Header("�������")]
    public AchievementCondition condition;
    [Header("ǰ������ID")]
    public int preId;
    [Header("����")]
    public int reward;
}

[CreateAssetMenu(fileName = "AchievementDataBase", menuName = "GameDataBase/AchievementDataBase")]
public class AchievementDataBase : ScriptableObject
{
    public List<AchievementData> data;
}
