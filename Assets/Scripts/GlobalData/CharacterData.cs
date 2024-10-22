using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class CharacterData
{
    //单例模式
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
    const int InitHp = 100;
    const int InitMp = 100;
    public int hp;
    public int mp;
    public int level;//没用到
    public int exp;//没用到

    public int MaxHpAdd;
    public int MaxHpMul;
    public int MaxMpAdd;
    public int MaxMpMul;
    public int MaxLevel;//没用到
    public int Damage;
    public int PinLv;
    public int SheCheng;
    public int FlySpeed;
    public int Lucky;//没用到
    public int XiXue;
    public int GetDamage;

    public int hpMax => (MaxHpAdd + InitHp) * MaxHpMul / 100;
    public int mpMax => (MaxMpAdd + InitMp) * MaxMpMul / 100;

    public int hpAdd { get { return 0; } set { hp = Mathf.Max(0, Mathf.Min(hpMax, hp + value)); } }
    public int mpAdd { get { return 0; } set { mp = Mathf.Max(0, Mathf.Min(mpMax, mp + value)); } }

    public int coinAdd { get { return 0; } set { GameData.Inst.AddValueByEnum(GameDataEnum.GlodCoin, value); } }
    
    public int LevelUpExp { get 
        {
            if (level == MaxLevel) return 1000000007;
            return (int)(level * math.log10(level) + 10);
        } }

    public int xpAdd { get { return 0; } set 
        {
            exp += value;
            while(exp >= LevelUpExp)
            {
                exp -= LevelUpExp;
                level++;
                //随机一个奖励
                GameManager.databasesManager.LevelUp();
            }
        } }

    public void Reset()
    {
        level = 1;
        exp = 0;
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

        TianFuDataInit();

        //最后加载被其他初始化影响的数据
        hp = hpMax;
        mp = mpMax;
    }

    public void TianFuDataInit()
    {
        for(int i = 0; i < GameManager.databasesManager.TianFuGetSize(); i++) 
        {
            var level = GameManager.databasesManager.TianFuGetLevel(i);
            if(level != 0)
            {
                var type = GameManager.databasesManager.tianfu.data[i].type;
                var value = GameManager.databasesManager.tianfu.data[i].value;
                AddValueByEnum(type, value * level);
            }
        }
    }

    public int GetValueByEnum(CharacterDataEnum name)
    {
        return (int)GetType().GetField(name.ToString()).GetValue(this);
    }

    public void AddValueByEnum(CharacterDataEnum name, int value)
    {
        GetType().GetField(name.ToString()).SetValue(this, GetValueByEnum(name) + value);
    }

    public int GetValueByAddEnum(CharacterAddDataEnum name)
    {
        if (GetType().GetField(name.ToString()) == null)
            return (int)GetType().GetProperty(name.ToString()).GetValue(this);
        return (int)GetType().GetField(name.ToString()).GetValue(this);
    }

    public void AddValueByAddEnum(CharacterAddDataEnum name, int value)
    {
        if (GetType().GetField(name.ToString()) == null)
            GetType().GetProperty(name.ToString()).SetValue(this, GetValueByAddEnum(name) + value);
        else
            GetType().GetField(name.ToString()).SetValue(this, GetValueByAddEnum(name) + value);
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

public enum CharacterAddDataEnum
{
    Damage,
    PinLv,
    SheCheng,
    FlySpeed,
    Lucky,
    XiXue,
    GetDamage,
    hpAdd,
    mpAdd,
    coinAdd,
    xpAdd,
}
