using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class AchievementItem : MonoBehaviour
{
    Text Title, Text;
    RewardItem RewardItem;
    int id;
    private void Awake()
    {
        Title = transform.Find("TitleText").GetComponent<Text>();
        Text = transform.Find("Text").GetComponent<Text>();
        RewardItem = transform.Find("RewardItem").GetComponent<RewardItem>();
    }

    public void Init(int id = -1)
    {
        if (id == -1)
            id = this.id;
        this.id = id;
        Title.text = GameManager.databasesManager.achievement.data[id].title;
        Text.text = GameManager.databasesManager.achievement.data[id].des;
        RewardItem.Init(id);
    }

    //排序优先级，优先级越大显示越靠前
    const int maxPriority = 1000000007;
    const int minPriority = -1000000007;
    public int SortPriority()
    {
        if (RewardItem.isFinished)
            return minPriority - id;
        else if (RewardItem.progress.x >= RewardItem.progress.y)
            return maxPriority - id;
        else return (int)(1.0f * RewardItem.progress.x / RewardItem.progress.y * 100000);
    }
}