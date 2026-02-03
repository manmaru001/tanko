using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameTimer : MonoBehaviour
{
    private float m_fTimer;

    // 現在の時刻を取得
    public float CurrentTime { get { return m_fTimer; } }

    // タイマーは有効かどうか
    public bool m_bActive = false;

    private void Update()
    {
        // タイマーが有効な場合、デルタタイムを追加する
        if (m_bActive)
        {
            m_fTimer += Time.deltaTime;
        }
    }

    // タイマーをスタートさせる
    public void OnStart()
    {
        m_bActive = true;
    }

    // タイマーを一時停止させる(ポーズメニューなどを作るとき用)
    public void OnStop()
    {
        m_bActive = false;
    }

    // タイマーをリセットさせる
    public void OnReset()
    {
        m_fTimer = 0f;
        OnStop();
    }
}