using TMPro;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.UI;

public class UpdatGameState : MonoBehaviour
{
   
    TextMeshProUGUI text;
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
        Character character = World.DefaultGameObjectInjectionWorld.EntityManager.GetComponentData<Character>(characterEntity);
        var hpMax = (character.hpMax + CharacterData.Inst.MaxHpAdd) * CharacterData.Inst.MaxHpMul / 100;
        var mpMax = (character.mpMax + CharacterData.Inst.MaxMpAdd) * CharacterData.Inst.MaxMpMul / 100;
        HPImage.fillAmount = 1.0f * character.hp / hpMax;
        MPImage.fillAmount = 1.0f * character.mp / mpMax;
        HPText.text = $"{character.hp}/{hpMax}";
        MPText.text = $"{character.mp}/{mpMax}";
        //显示游玩时间
        WorldData.Inst.totalSeconds += Time.deltaTime;
        LastTimeText.text = string.Format("{0:D2} : {1:D2}", WorldData.Inst.minute, WorldData.Inst.second);

    }
}
