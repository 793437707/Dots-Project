using Unity.Entities;
using UnityEngine;

class GameManager : MonoBehaviour
{
    public int FPSMax = 60;
    private void Awake()
    {
        Application.targetFrameRate = FPSMax;
    }
}