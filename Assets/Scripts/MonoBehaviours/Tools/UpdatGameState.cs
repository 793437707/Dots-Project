using TMPro;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.UI;

public class UpdatGameState : MonoBehaviour
{
   
    Transform HP, MP, XP;
    Image HPImage, MPImage, XPImage;
    Text HPText, MPText, LastTimeText, XPText;

    void Start()
    {
        HP = transform.Find("HP");
        MP = transform.Find("MP");
        XP = transform.Find("XP");
        HPImage = HP.Find("Bar").GetComponent<Image>();
        MPImage = MP.Find("Bar").GetComponent<Image>();
        XPImage = XP.Find("Bar").GetComponent<Image>();
        HPText = HP.Find("Text").GetComponent<Text>();
        MPText = MP.Find("Text").GetComponent<Text>();
        XPText = XP.Find("num").GetComponent<Text>();
        LastTimeText = transform.Find("LastTimeText").GetComponent<Text>();
    }

    void LateUpdate()
    {
        Entity characterEntity = GameManager.GetEntityForTag("Character");
        if (characterEntity == Entity.Null) return;

        //显示血条蓝条
        HPImage.fillAmount = 1.0f * CharacterData.Inst.hp / CharacterData.Inst.hpMax;
        MPImage.fillAmount = 1.0f * CharacterData.Inst.mp / CharacterData.Inst.mpMax;
        HPText.text = $"{CharacterData.Inst.hp}/{CharacterData.Inst.hpMax}";
        MPText.text = $"{CharacterData.Inst.mp}/{CharacterData.Inst.mpMax}";
        //显示游玩时间，难度
        WorldData.Inst.totalSeconds += Time.deltaTime;
        LastTimeText.text = string.Format("{0:D2} : {1:D2}\nScore:{2}   Different:{3:N1}", WorldData.Inst.minute, WorldData.Inst.second, WorldData.Inst.totalScore, WorldData.Inst.different);
        //更新等级，显示等级信息
        XPText.text = CharacterData.Inst.level.ToString();
        XPImage.fillAmount = 1.0f - 1.0f * CharacterData.Inst.exp / CharacterData.Inst.LevelUpExp;
    }
}
