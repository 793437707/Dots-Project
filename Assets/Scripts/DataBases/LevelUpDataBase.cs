using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;



[Serializable]
public struct LevelUpData
{
    [Header("加强玩家属性的类型")]
    public CharacterDataEnum type;
    [Header("增加多少玩家属性")]
    public int value;
    [Header("文字描述")]
    public string text;
}

[CreateAssetMenu(fileName = "LevelUpDataBase", menuName = "GameDataBase/LevelUpDataBase")]
public class LevelUpDataBase : ScriptableObject
{
    public List<LevelUpData> data;
}