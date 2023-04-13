using TMPro;
using UnityEngine;

public class ShowFPS : MonoBehaviour
{
    TextMeshProUGUI text;
    float _updateInterval = 1f;//�趨����֡�ʵ�ʱ����Ϊ1��  
    float _accum = .0f;//�ۻ�ʱ��  
    int _frames = 0;//��_updateIntervalʱ���������˶���֡  
    float _timeLeft;

    void Start()
    {
        text = GetComponent<TextMeshProUGUI>();
        _timeLeft = _updateInterval;
    }

    void Update()
    {
        _timeLeft -= Time.deltaTime;
        //Time.timeScale���Կ���Update ��LateUpdate ��ִ���ٶ�,  
        //Time.deltaTime��������㣬������һ֡��ʱ��  
        //������ɵõ���Ӧ��һ֡���õ�ʱ��  
        _accum += Time.timeScale / Time.deltaTime;
        ++_frames;//֡��  

        if (_timeLeft <= 0)
        {
            float fps = _accum / _frames;
            text.text = System.String.Format("FPS: {0:F2}", fps);//������λС��  

            _timeLeft = _updateInterval;
            _accum = .0f;
            _frames = 0;
        }
    }
}
