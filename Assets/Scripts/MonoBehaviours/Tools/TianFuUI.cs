using NUnit.Framework;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;


public class TianFuUI : MonoBehaviour
{
    public GameObject prefab;
    bool isInit = false;
    Transform Content;
    List<TianFuItem> items = new List<TianFuItem>();
    Text CoinText;
    Vector3 pos;
    int coin;

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
        if (!isInit)
        {
            InitItems();
            isInit = true;
        }
        for (int i = 0; i < items.Count; i++)
        {
            items[i].Init();
        }
        Content.GetComponent<RectTransform>().position = pos;
        coin = -1;
        UpdateCoin();
    }

    void UpdateCoin()
    {
        if (coin == GameData.Inst.GlodCoin)
            return;
        //金币变动，更新显示属性以及能否购买
        coin = GameData.Inst.GlodCoin;
        CoinText.text = coin.ToString();
        for (int i = 0; i < items.Count; i++)
        {
            items[i].UpdateColor();
        }
    }

    void InitItems()
    {
        for (int i = 0; i < GameManager.databasesManager.TianFuGetSize(); i++)
        {
            var item = Instantiate(prefab, Content);
            items.Add(item.GetComponent<TianFuItem>());
            item.GetComponent<TianFuItem>().Init(i);
        }
    }
}