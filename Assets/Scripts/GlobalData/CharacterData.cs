using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterData
{
    //����ģʽ
    private static CharacterData inst;
    public static CharacterData Inst
    {
        get
        {
            if (inst == null)
            {
                inst = new CharacterData();
            }
            return inst;
        }
    }
    public int MaxHpAdd;
    public int MaxHpMul;
    public int MaxMpAdd;//û�õ�
    public int MaxMpMul;//û�õ�
    public int MaxLevel;//û�õ�
    public int Damage;
    public int PinLv;
    public int SheCheng;
    public int FlySpeed;
    public int Lucky;//û�õ�
    public int XiXue;
    public int GetDamage;

    public void Reset()
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

public enum CharacterDataEnum
{
    MaxHpAdd,
    MaxHpMul,
    MaxMpAdd,
    MaxMpMul,
    MaxLevel,
    Damage,
    PinLv,
    SheCheng,
    FlySpeed,
    Lucky,
    XiXue,
    GetDamage,
}
