// i yanked this class from the internet... stackoverflow or something.
// i added the ability to change color and font size
// i added the ability to set target frame rate and vsync count.
// i added the ability to set where on the screen the text is anchored.
//--------------------------------------------------------------------------------------------------//

using UnityEngine;

public class FpsDisplay : MonoBehaviour
{
	[Tooltip("An integer number for font size that scales with screen size, 1..n (default 2)")] public int m_fontSize = 4;
	[Tooltip("Color of the text to use (default null)")] public Color m_textColor = new Color(0.0f, 0.0f, 0.5f, 1.0f);
	[Tooltip("The target frame-rate for the app. If set it to zero, the rate won't be set.  (default 0)")] public int m_appTargetFrameRate = 0;
	[Tooltip("The vsync count for the app. If set to zero, the count won't be set. (default 0)")] public int m_vSyncCount = 0;
	[Tooltip("Determines how the text is aligned on the screen. (default UpperLeft)")] public TextAnchor m_alignment = TextAnchor.UpperLeft;

	float deltaTime = 0.0f;
	float m_y;
	float m_height;

	private void Awake()
    {
		if (m_appTargetFrameRate >= 0) { Application.targetFrameRate = m_appTargetFrameRate; }
		if (m_vSyncCount >= 0) { QualitySettings.vSyncCount = m_vSyncCount; }

		m_height = (Screen.height * m_fontSize) / 100;
		if ((m_alignment == TextAnchor.LowerCenter) || (m_alignment == TextAnchor.LowerLeft) || (m_alignment == TextAnchor.LowerRight)) {
			m_y = Screen.height - m_height;
		} else if ((m_alignment == TextAnchor.MiddleCenter) || (m_alignment == TextAnchor.MiddleLeft) || (m_alignment == TextAnchor.MiddleRight)) {
			m_y = (Screen.height / 2f) - m_height;
		}
	}

    void Update()
	{
		deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
	}

	void OnGUI()
	{
		int h = Screen.height;
		GUIStyle style = new GUIStyle();
		Rect rect = new Rect(0, m_y, Screen.width, m_height);
		style.alignment = m_alignment;
		style.fontSize = (int)m_height;
		style.normal.textColor = m_textColor;
		float msec = deltaTime * 1000.0f;
		float fps = 1.0f / deltaTime;
		string text = string.Format("{0:0.0} ms, {1:0.} fps", msec, fps);
		GUI.Label(rect, text, style);
	}
}