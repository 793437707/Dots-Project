using System.Collections;
using UnityEngine;

public class UIManager : MonoBehaviour
{

    private GameObject Main, Setting, Loading, Game;

    private void Awake()
    {
        GameManager.uIManager = this;
        Main = transform.Find("Main").gameObject;
        Setting = transform.Find("Setting").gameObject;
        Loading = transform.Find("Loading").gameObject;
        Game = transform.Find("Game").gameObject;

        Main.SetActive(true);
        Setting.SetActive(false);
        Loading.SetActive(false);
        Game.SetActive(false);
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
}