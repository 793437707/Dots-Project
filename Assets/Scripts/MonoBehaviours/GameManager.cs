using Unity.Entities;
using UnityEngine;

class GameManager : MonoBehaviour
{
    [Tooltip("The target frame-rate for the app. If set it to zero, the rate won't be set.  (default 0)")] public int m_appTargetFrameRate = 0;
    [Tooltip("The vsync count for the app. If set to zero, the count won't be set. (default 0)")] public int m_vSyncCount = 0;

    public static bool MouseAutoSpawn;

    private void Awake()
    {
        if (m_appTargetFrameRate >= 0) { Application.targetFrameRate = m_appTargetFrameRate; }
        if (m_vSyncCount >= 0) { QualitySettings.vSyncCount = m_vSyncCount; }
        LoadSetting();
    }

    private void LoadSetting()
    {
        MouseAutoSpawn = true;
    }
}