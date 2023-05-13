using Lean.Gui;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using System.Text;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{

    private GameObject Main, Setting, Loading, Game, Dead, Option;
    private Queue<string> message;
    Text LoadingText, MessageText;

    private void Awake()
    {
        GameManager.uIManager = this;
        Main = transform.Find("Main").gameObject;
        Setting = transform.Find("Setting").gameObject;
        Loading = transform.Find("Loading").gameObject;
        Game = transform.Find("Game").gameObject;
        Dead = transform.Find("Dead").gameObject;
        Option = transform.Find("Option").gameObject;

        LoadingText = Loading.transform.Find("Loading").GetComponent<Text>();
        MessageText = Game.transform.Find("MessageText").GetComponent<Text>();

        message = new Queue<string>();

        Main.SetActive(true);
        Setting.SetActive(false);
        Loading.SetActive(false);
        Game.SetActive(false);
        Dead.SetActive(false);
        Option.SetActive(false);
    }

    
    public void SetLoadingText(string text)
    {
        LoadingText.text = text;
    }

    public void StartGame()
    {
        GameManager.gameManager.LoadGameScene();
    }


    public void ExitGameInMain()
    {

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void ExitGameInGame()
    {
        GameManager.gameManager.UnloadGameScene();
    }


    public void SwitchPauseInGame()
    {
        GameManager.gameManager.SwitchPause();
    }

    public void SaveLocalGameData()
    {
        GameManager.gameManager.SaveLocalGameData();
    }

    public void ShowDead()
    {
        Game.transform.Find("SettingBtn").gameObject.SetActive(false);
        Dead.SetActive(true);


        System.Text.StringBuilder str = new System.Text.StringBuilder();
        str.Append("本轮得分：");
        str.Append(WorldData.Inst.totalScore);
        if (GameData.Inst.MaxScore == WorldData.Inst.totalScore)
            str.Append("新纪录！");
        str.Append('\n');

        str.Append("历史最高得分：");
        str.Append(GameData.Inst.MaxScore);
        str.Append("\n");

        str.Append("本轮用时：");
        str.Append(string.Format("{0:D2} : {1:D2}", WorldData.Inst.minute, WorldData.Inst.second));
        if (GameData.Inst.MaxPlayTime == WorldData.Inst.totalSeconds)
            str.Append("新纪录！");
        str.Append("\n");

        str.Append("历史最长游戏：");
        str.Append(string.Format("{0:D2} : {1:D2}", GameData.Inst.MaxPlayTime / 60, GameData.Inst.MaxPlayTime % 60));
        str.Append("\n");

        str.Append("随机种子：");
        str.Append(GameData.Inst.MapSeed);
        str.Append("\n");

        Dead.transform.Find("DeadUI/ScoreText").GetComponent<Text>().text = str.ToString();
    }
    public void LoadingToGame()
    {
        Loading.SetActive(false);
        Game.SetActive(true);
        message.Clear();
        MessageText.text = "";
    }
    public void LoadingToMain()
    {
        Loading.SetActive(false);
        Main.SetActive(true);
    }
    
    public void LoadOption()
    {
        for(int i = 0; i < GameManager.OptionSize; i ++)
        {
            LeanToggle toggle = Option.transform.Find("Toggle" + i).GetComponent<LeanToggle>();
            bool value = GameData.Inst.Option[i];
            toggle.InitAndTurn(value);
        }
    }

    public void ChangeOption(int value)
    {
        int id = value / 100;
        bool modify = value % 100 == 1;
        if (GameData.Inst.Option[id] == modify) return;
        GameData.Inst.Option[id] = modify;
        GameData.Inst.SavaData();
    }

    public void SetFullScreen()
    {
        if(GameData.Inst.Option[3])
        {
            Resolution[] resolutions = Screen.resolutions;
            Screen.SetResolution(resolutions[resolutions.Length - 1].width, resolutions[resolutions.Length - 1].height, true);
        }
        else
        {
            Screen.SetResolution(1600, 900, false);
        }
        
    }

    public void SetVSync()
    {
        if (GameData.Inst.Option[4])
        {
            QualitySettings.vSyncCount = 1;
        }
        else
        {
            QualitySettings.vSyncCount = 0;
        }
    }

    void AddMessage(string text)
    {
        message.Enqueue(string.Format("[{0:D2}:{1:D2}]{2}", WorldData.Inst.minute, WorldData.Inst.second, text));
        if (message.Count > 6)
            message.Dequeue();
        StringBuilder str = new StringBuilder();
        foreach(string t in message)
        {
            str.AppendLine(t);
        }
        MessageText.text = str.ToString();
    }
}