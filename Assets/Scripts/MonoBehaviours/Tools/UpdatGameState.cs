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
    Text HPText, MPText;

    void Start()
    {
        HP = transform.Find("HP");
        MP = transform.Find("MP");
        HPImage = HP.Find("Bar").GetComponent<Image>();
        MPImage = MP.Find("Bar").GetComponent<Image>();
        HPText = HP.Find("Text").GetComponent<Text>();
        MPText = MP.Find("Text").GetComponent<Text>();
    }

    void LateUpdate()
    {
        Entity characterEntity = GameManager.GetEntityForTag("Character");
        if(characterEntity == Entity.Null)
        {
            HPImage.fillAmount = 1;
            MPImage.fillAmount = 1;
            HPText.text = "?/?";
            MPText.text = "?/?";
            return;
        }
        Character character = World.DefaultGameObjectInjectionWorld.EntityManager.GetComponentData<Character>(characterEntity);
        var hpMax = (character.hpMax + CharacterData.MaxHpAdd) * CharacterData.MaxHpMul / 100;
        var mpMax = (character.mpMax + CharacterData.MaxMpAdd) * CharacterData.MaxMpMul / 100;
        HPImage.fillAmount = 1.0f * character.hp / hpMax;
        MPImage.fillAmount = 1.0f * character.mp / mpMax;
        HPText.text = $"{character.hp}/{hpMax}";
        MPText.text = $"{character.mp}/{mpMax}";
    }
}
