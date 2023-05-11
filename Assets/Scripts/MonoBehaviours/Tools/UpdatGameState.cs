using TMPro;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.UI;

public class UpdatGameState : MonoBehaviour
{
   
    Transform HP, MP;
    Image HPImage, MPImage;
    Text HPText, MPText, LastTimeText;

    void Start()
    {
        HP = transform.Find("HP");
        MP = transform.Find("MP");
        HPImage = HP.Find("Bar").GetComponent<Image>();
        MPImage = MP.Find("Bar").GetComponent<Image>();
        HPText = HP.Find("Text").GetComponent<Text>();
        MPText = MP.Find("Text").GetComponent<Text>();
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
        //显示游玩时间
        WorldData.Inst.totalSeconds += Time.deltaTime;
        LastTimeText.text = string.Format("{0:D2} : {1:D2}\nScore:{2}", WorldData.Inst.minute, WorldData.Inst.second, WorldData.Inst.totalScore);

    }
}
