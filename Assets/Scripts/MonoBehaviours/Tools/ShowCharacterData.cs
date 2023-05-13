using System.Text;
using UnityEngine.UI;
using System;
using UnityEngine;

public class ShowCharacterData : MonoBehaviour
{

    Text text;
    StringBuilder str;

    void Awake()
    {
        text = GetComponent<Text>();
        str = new StringBuilder();
    }

    void Update()
    {
        str.Clear();
        str.Append("伤害：");
        str.Append(CharacterData.Inst.Damage);
        str.Append("%\n");

        str.Append("护甲：");
        str.Append(100 - CharacterData.Inst.GetDamage);
        str.Append("%\n");

        str.Append("吸血：");
        str.Append(CharacterData.Inst.XiXue);
        str.Append("%\n");

        str.Append("幸运：");
        str.Append(CharacterData.Inst.Lucky);
        str.Append("%\n");

        str.Append("法球射程：");
        str.Append(CharacterData.Inst.SheCheng);
        str.Append("%\n");

        str.Append("法球发射CD：");
        str.Append(CharacterData.Inst.PinLv);
        str.Append("%\n");

        str.Append("法球飞行速度：");
        str.Append(CharacterData.Inst.FlySpeed);
        str.Append("%\n");

        text.text = str.ToString();
    }
}
