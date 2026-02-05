using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JewelryController : MonoBehaviour
{
    // 再生用（Inspector でセット可能）
    [Header("Sound")]
    public GameObject SoundManager;      // シーン上の SoundManager オブジェクト（任意）
    public AudioClip pickupSound;        // フォールバック用の音（任意）

    void Start()
    {
        // 既にセットされていなければシーンから探す（SoundManager コンポーネントを探してその GameObject を取得）
        if (SoundManager == null)
        {
            var smComp = FindFirstObjectByType<SoundManager>();
            if (smComp != null)
            {
                SoundManager = smComp.gameObject;
            }
        }
    }

    //爆発範囲内にあったらオブジェクトを破壊&スコア増加
    public void ExplodeJewelry()
    {
        // 爆発時にも音を鳴らしたければここでも再生できます（必要ならアンコメント）
        PlayPickupSound();

        Destroy(gameObject);
        ScoreManagerSingleton.instance.m_score++;
    }

    //プレイヤータグと衝突したらオブジェクトを破壊
    private void OnTriggerEnter2D(Collider2D collision)
    {
        
        if (collision.CompareTag("player"))
        {
            // サウンド再生
            PlayPickupSound();

            Destroy(gameObject);
            ScoreManagerSingleton.instance.m_score++;
        }
    }

    // サウンド再生ヘルパー
    void PlayPickupSound()
    {
        // まず SoundManager 経由を試す（PlaySFX(string, volume, pitch) を使える前提）
        if (SoundManager != null)
        {
            var sm = SoundManager.GetComponent<SoundManager>();
            if (sm != null)
            {
                // ランダムなピッチで少しバリエーションを付ける
                float randomPitch = Random.Range(0.9f, 1.1f);

                //再生
                //sm.PlaySFX("Sound_Jewerly", 1.0f, randomPitch);
                sm.PlaySFX("Sound_Jewel", 1.0f, randomPitch);

                return;
            }
        }

        // フォールバック：Inspector にセットした AudioClip を使う
        if (pickupSound != null)
        {
            AudioSource.PlayClipAtPoint(pickupSound, transform.position, 1.0f);
        }
    }
}
