using NUnit.Framework;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;


public class AchievementUI : MonoBehaviour
{
    public GameObject prefab;
    bool isInit = false;
    Transform Content;
    List<AchievementItem> items = new List<AchievementItem>();
    Vector3 pos;

    private void Awake()
    {
        Content = transform.Find("Scroll View/Viewport/Content");
        pos = Content.GetComponent<RectTransform>().position;
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