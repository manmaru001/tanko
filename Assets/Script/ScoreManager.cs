using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public class ScoreManager : MonoBehaviour
{
    private Text m_txt;
    public int m_score = 0;
    public string m_strFormat;

    // Start is called before the first frame update
    void Start()
    {
        // テキストを取得
        m_txt = GetComponent<Text>();

        m_score = 0;
    }

    // Update is called once per frame
    void Update()
    {
        // テキストとして出力
        m_txt.text = string.Format(m_strFormat, ScoreManagerSingleton.instance.m_score);
    }
}
