using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;



[Serializable]
public struct LevelUpData
{
    [Header("��ǿ������Ե�����")]
    public CharacterDataEnum type;
    [Header("���Ӷ����������")]
    public int value;
    [Header("��������")]
    public string text;
}

[CreateAssetMenu(fileName = "LevelUpDataBase", menuName = "GameDataBase/LevelUpDataBase")]
public class LevelUpDataBase : ScriptableObject
{
    public List<LevelUpData> data;
}