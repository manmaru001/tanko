using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public class ShowCountdown : MonoBehaviour
{
    public float m_fStartTime;
    public string m_strFormat;
    public GameTimer m_gameTimer;

    private Text m_txt;

    private void Start()
    {
        m_txt = GetComponent<Text>();
    }

    private void Update()
    {
        float fShowTime = Mathf.Clamp(m_fStartTime - m_gameTimer.CurrentTime, 0f, m_fStartTime);
        m_txt.text = string.Format(m_strFormat, fShowTime);
    }
}