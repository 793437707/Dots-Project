using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;



[Serializable]
public struct TianFuData
{
    [Header("标题，天赋名称")]
    public string title;
    [Header("描述")]
    public string des;
    [Header("每级消耗金币数，对应等级上限，不能为空")]
    public int[] cost;
    [Header("图标ID 对应images的下标")]
    public int iconID;
    [Header("加强玩家属性的类型")]
    public CharacterDataEnum type;
    [Header("每级增加多少玩家属性")]
    public int value;
}

[CreateAssetMenu(fileName = "TianFuDataBase", menuName = "GameDataBase/TianFuDataBase")]
public class TianFuDataBase : ScriptableObject
{
    public List<TianFuData> data;
    public List<Sprite> images;
}