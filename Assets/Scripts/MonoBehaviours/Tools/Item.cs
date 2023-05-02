using System.Collections;
using UnityEngine;
using UnityEngine.UI;


public class Item : MonoBehaviour
{
    GameObject Ignore, Reward;
    Text num;

    private void Awake()
    {
        Ignore = transform.Find("Ignore").gameObject;
        Reward = transform.Find("Reward").gameObject;
        num = transform.Find("Num").GetComponent<Text>();
        Ignore.SetActive(false);
        Reward.SetActive(false);
    }

    public void SetIgnore()
    {
        Ignore.SetActive(true);
        Reward.SetActive(false);
    }

    public void SetReward()
    {
        Ignore.SetActive(false);
        Reward.SetActive(true);
    }

    public void SetNumText(string txt)
    {
        num.text = txt;
    }
}