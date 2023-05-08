using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{

    private GameObject Main, Setting, Loading, Game, Dead;

    private void Awake()
    {
        GameManager.uIManager = this;
        Main = transform.Find("Main").gameObject;
        Setting = transform.Find("Setting").gameObject;
        Loading = transform.Find("Loading").gameObject;
        Game = transform.Find("Game").gameObject;
        Dead = transform.Find("Dead").gameObject;

        LoadingText = Loading.transform.Find("Loading").GetComponent<Text>();

        Main.SetActive(true);
        Setting.SetActive(false);
        Loading.SetActive(false);
        Game.SetActive(false);
        Dead.SetActive(false);
    }

    Text LoadingText;
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
    }
    public void LoadingToGame()
    {
        Loading.SetActive(false);
        Game.SetActive(true);
    }
    public void LoadingToMain()
    {
        Loading.SetActive(false);
        Main.SetActive(true);
    }
}