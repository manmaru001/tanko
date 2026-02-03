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

    // 残りタイムが0になった時のフラグ
    public static bool m_cuntZero = false;

    private void Start()
    {
        // テキストを取得
        m_txt = GetComponent<Text>();

        // カウントは0じゃない
        m_cuntZero = false;
    }

    private void Update()
    {
        // ゲームタイマ―から取得された値を取得、代入しカウントへ適応
        float fShowTime = Mathf.Clamp(m_fStartTime - m_gameTimer.CurrentTime, 0f, m_fStartTime);


        // カウントが0になったフラグを立てる
        if (fShowTime == 0)
        {
            m_cuntZero = true;
        }

        // テキストとして出力
        m_txt.text = string.Format(m_strFormat, fShowTime);
    }
}