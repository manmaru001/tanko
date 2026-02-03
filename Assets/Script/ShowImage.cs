using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShowImage : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] private GameObject a;//GameObject型の変数aを宣言　好きなゲームオブジェクトをアタッチ

    public GameTimer m_gameTimer;

    void Start()
    {
        a.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (ShowCountdown.CountZero == true)
        a.SetActive(true);
    }

    public void OnRetryButton()
    {
        // フラグを元に戻す
        ShowCountdown.CountZero = false;

        // タイマーをリセットして、すぐにスタートさせる
        m_gameTimer.OnReset();
        m_gameTimer.OnStart();

        // 画像を非表示にする
        a.SetActive(false);
    }
}
