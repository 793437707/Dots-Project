using NUnit.Framework;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;


public class AchievementUI : MonoBehaviour
{
    public GameObject prefab;
    bool isInit = false;
    Transform Content;
    List<AchievementItem> items = new List<AchievementItem>();
    Text CoinText;
    Vector3 pos;
    int coin = -1;

    private void Awake()
    {
        Content = transform.Find("Scroll View/Viewport/Content");
        pos = Content.GetComponent<RectTransform>().position;
        CoinText = transform.Find("CoinItem/cost").GetComponent<Text>();
    }

    private void LateUpdate()
    {
        UpdateCoin();
    }

    public void Show()
    {
        if(!isInit)
        {
            InitItems();
            isInit = true;
        }
        for(int i = 0; i < items.Count; i ++)
        {
            items[i].Init();
        }
        Content.GetComponent<RectTransform>().position = pos;
        //排序显示成就
        items.Sort((a, b) => b.SortPriority().CompareTo(a.SortPriority()));
        for(int i = 0; i < items.Count; i ++)
        {
            items[i].transform.SetSiblingIndex(i);
        }
    }

    void UpdateCoin()
    {
        if (coin == GameData.Inst.GlodCoin)
            return;
        //金币变动，更新显示属性以及能否购买
        coin = GameData.Inst.GlodCoin;
        CoinText.text = coin.ToString();
    }

    void InitItems()
    {
        for(int i = 0; i < GameManager.databasesManager.AchievementGetSize(); i ++)
        {
            var item = Instantiate(prefab, Content);
            items.Add(item.GetComponent<AchievementItem>());
            item.GetComponent<AchievementItem>().Init(i);
        }
    }
}