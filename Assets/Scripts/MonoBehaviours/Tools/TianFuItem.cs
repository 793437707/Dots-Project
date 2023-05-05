using Lean.Gui;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TianFuItem : MonoBehaviour
{
    Text Title, Text, ProgressText, LevelDownCost, LevelUpCost;
    Image Icon, Bar;
    GameObject LevelDownNone, LevelUpNone, LevelDown, LevelUp;
    Transform BuyItem;
    LeanButton LevelDownButton, LevelUpButton;
    int id;
    int level, maxLevel;
    private void Awake()
    {
        Title = transform.Find("TitleText").GetComponent<Text>();
        Text = transform.Find("Text").GetComponent<Text>();
        Icon = transform.Find("Icon").GetComponent<Image>();
        BuyItem = transform.Find("TianFuBuyItem");
        ProgressText = BuyItem.Find("ProgressText").GetComponent<Text>();
        Bar = BuyItem.Find("ProgressImg/Bar").GetComponent<Image>();
        LevelDown = BuyItem.Find("LevelDown").gameObject;
        LevelDownButton = LevelDown.transform.Find("Button").GetComponent<LeanButton>();
        LevelDownCost = LevelDown.transform.Find("CoinItem/cost").GetComponent<Text>();
        LevelDownNone = BuyItem.Find("LevelDownNone").gameObject;
        LevelUp = BuyItem.Find("LevelUp").gameObject;
        LevelUpNone = BuyItem.Find("LevelUpNone").gameObject;
        LevelUpButton = LevelUp.transform.Find("Button").GetComponent<LeanButton>();
        LevelUpCost = LevelUp.transform.Find("CoinItem/cost").GetComponent<Text>();

        LevelDownButton.OnClick.AddListener(() =>
        {
            GameManager.databasesManager.TianfuAddLevel(id, -1);
            Init();
        });
        LevelUpButton.OnClick.AddListener(() =>
        {
            if(LevelUpCost.color == Color.red)
                return;
            GameManager.databasesManager.TianfuAddLevel(id, 1);
            Init();
        });
    }

    public void Init(int id = -1)
    {
        if (id == -1)
            id = this.id;
        this.id = id;
        Title.text = GameManager.databasesManager.tianfu.data[id].title;
        Text.text = GameManager.databasesManager.tianfu.data[id].des;
        Icon.sprite = GameManager.databasesManager.TianFuGetIcon(id);
        level = GameManager.databasesManager.TianFuGetLevel(id);
        maxLevel = GameManager.databasesManager.TianFuGetMaxLevel(id);
        ProgressText.text = $"{level}/{maxLevel}";
        Bar.fillAmount = 1.0f * level / maxLevel;
        LevelDown.SetActive(level != 0);
        LevelDownNone.SetActive(level == 0);
        LevelUp.SetActive(level != maxLevel);
        LevelUpNone.SetActive(level == maxLevel);
        LevelDownCost.text = $"+ {(level != 0 ? GameManager.databasesManager.TianFuGetCost(id, level) : 0)}";
        LevelUpCost.text = $"- {(level != maxLevel ? GameManager.databasesManager.TianFuGetCost(id, level + 1) : 0)}";
        LevelUpCost.color = level != maxLevel && GameManager.databasesManager.TianFuGetCost(id, level + 1) > GameData.Inst.GlodCoin ? Color.red : Color.white;
    }
}