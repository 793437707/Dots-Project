using Lean.Gui;
using System.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

public class RewardItem : MonoBehaviour
{
    GameObject BGCanReward;
    Text ProgressText;
    Image Bar;
    LeanButton Button;
    Item item;
    public int2 progress;
    public bool isFinished;
    int id = -1;
    private void Awake()
    {
        BGCanReward = transform.Find("BGCanReward").gameObject;
        ProgressText = transform.Find("ProgressText").GetComponent<Text>();
        Bar = transform.Find("ProgressImg/Bar").GetComponent<Image>();
        Button = transform.Find("Button").GetComponent<LeanButton>();
        item = transform.Find("Item").GetComponent<Item>();
        Button.OnClick.RemoveAllListeners();
        Button.OnClick.AddListener(OnButtonDown);
    }

    public void Init(int id)
    {
        this.id = id;
        progress = GameManager.databasesManager.AchievementProgress(id);
        isFinished = GameManager.databasesManager.AchievementIsFinished(id);
        SetProgress(progress, isFinished);
        var reward = GameManager.databasesManager.achievement.data[id].reward;
        item.SetNumText($"x{reward}");
    }

    public void SetProgress(int2 progress, bool isFinished)
    {
        ProgressText.text = $"{progress.x}/{progress.y}";
        Bar.fillAmount = 1.0f * progress.x / progress.y;
        if (progress.x >= progress.y && !isFinished)
        {
            BGCanReward.SetActive(true);
            Button.interactable = true;
        }
        else
        {
            BGCanReward.SetActive(false);
            Button.interactable = false;
        }

        if (isFinished) item.SetReward();
        else if (progress.x < progress.y) item.SetIgnore();
    }

    void OnButtonDown()
    {
        if(id == -1)
        {
            Debug.LogError("No Init Id!");
            return;
        }
        GameManager.databasesManager.AchievementFinish(id);
        item.SetReward();
        BGCanReward.SetActive(false);
        Button.interactable = false;
    }
}