using System.Collections;
using System.IO;
using UnityEngine;

public class GMManager : MonoBehaviour
{

    private void Awake()
    {
        GameManager.gmManager = this;
    }

    public bool WuDi = false;
    public bool ManJi = false;
    public bool NanDu = false;

    public void ClearGameData()
    {
        GameData.Inst.ClearData();
        Debug.LogWarning("GM:ClearGameData");
    }

    public void INFCoin()
    {
        GameData.Inst.GlodCoin = 99999999;
        GameData.Inst.SavaData();
        Debug.LogWarning("GM:INFCoin");
    }

    public void AllAchievement()
    {
        GameData.Inst.MaxPlayTime = 99999999;
        GameData.Inst.PlayTimes = 99999999;
        GameData.Inst.TotalPlayTime = 99999999;
        GameData.Inst.MaxScore = 99999999;
        GameData.Inst.TotalScore = 99999999;
        GameData.Inst.SavaData();
        Debug.LogWarning("GM:AllAchievement");
    }

    public void TianFuMaxLevel()
    {
        for(int i = 0; i < GameManager.databasesManager.TianFuGetSize(); i++)
        {
            GameData.Inst.TianFuLevel[i] = GameManager.databasesManager.TianFuGetMaxLevel(i);
        }
        GameData.Inst.SavaData();
        Debug.LogWarning("GM:TianFuMaxLevel");
    }

    public void ChangeOption(int value)
    {
        int id = value / 100;
        bool modify = value % 100 == 1;
        switch(id)
        {
            case 1: WuDi = modify;break;
            case 2: ManJi = modify;break;
            case 3: NanDu = modify;break;
        }
        Debug.LogWarning("GM:GhangeOption:" + id + ":" + (modify ? "true" : "false"));
    }
}