using System.Collections;
using UnityEngine;

public class UIManager : MonoBehaviour
{

    private GameObject Main, Setting, Loading;

    private void Awake()
    {
        GameManager.uIManager = this;
        Main = transform.Find("Main").gameObject;
        Setting = transform.Find("Setting").gameObject;
        Loading = transform.Find("Loading").gameObject;

        Main.SetActive(true);
        Setting.SetActive(false);
        Loading.SetActive(false);
    }



    public void StartGame()
    {
        Main.SetActive(false);
        GameManager.gameManager.LoadGameScene();
    }

    public void SwitchSetting()
    {
        Setting.SetActive(!Setting.activeSelf);
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
        Main.SetActive(true);
        GameManager.gameManager.UnloadGameScene();
    }

    public void SwitchPauseInGame()
    {
        GameManager.gameManager.SwitchPause();
    }
}