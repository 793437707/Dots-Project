using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterData
{
    public static int MaxHpAdd;
    public static int MaxHpMul;
    public static int MaxMpAdd;//û�õ�
    public static int MaxMpMul;//û�õ�
    public static int MaxLevel;//û�õ�
    public static int Damage;
    public static int PinLv;
    public static int SheCheng;
    public static int FlySpeed;
    public static int Lucky;//û�õ�
    public static int XiXue;
    public static int GetDamage;

    public static void Reset()
    {
        MaxHpAdd = 0;
        MaxHpMul = 100;
        MaxMpAdd = 0;
        MaxMpMul = 100;
        MaxLevel = 50;
        Damage = 100;
        PinLv = 100;
        SheCheng = 100;
        FlySpeed = 100;
        Lucky = 100;
        XiXue = 0;
        GetDamage = 100;
    }
}
