using TMPro;
using UnityEngine;

public class ShowFPS : MonoBehaviour
{
   
    TextMeshProUGUI text;
    float _updateInterval = 1f;//设定更新帧率的时间间隔为1秒  
    float _timeLeft;
    float deltaTime = 0.0f;

    void Start()
    {
        text = GetComponent<TextMeshProUGUI>();
        _timeLeft = _updateInterval;
    }

    void Update()
    {
        _timeLeft -= Time.deltaTime;
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;

        if (_timeLeft <= 0)
        {
            float msec = deltaTime * 1000.0f;
            float fps = 1.0f / deltaTime;
            text.text = string.Format("{0:0.0} ms, {1:0.} fps", msec, fps);

            _timeLeft = _updateInterval;
        }
    }
}
