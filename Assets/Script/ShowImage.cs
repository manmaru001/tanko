using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShowImage : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] private GameObject a;//GameObject型の変数aを宣言　好きなゲームオブジェクトをアタッチ

    [SerializeField] private GameObject miningEnhancement; // 採掘強化ボタン
    [SerializeField] private GameObject bomEnhancement;    // 爆弾強化ボタン
    [SerializeField] private GameObject speedEnhancement;  // スピード強化ボタン

    public GameTimer m_gameTimer;
    public PlayerController m_playerController;
    public BombController m_bombController;
    [SerializeField] private Fade m_fade;

    //ScoreManager m_scoreManager;
    LevelManager m_levelManager;

    private bool hasFadedOut = false;

    //準備段階プレイヤー行動許可フラグ
    public bool m_readyPlayerAction = false;

    int m_miningLevel = 1;
    int m_bomLevel = 1;
    int m_speedLevel = 1;

    void Start()
    {
        //m_scoreManager = FindFirstObjectByType<ScoreManager>();
        m_levelManager = FindFirstObjectByType<LevelManager>();

        // 画像を非表示にする
        a.SetActive(false);

        miningEnhancement.SetActive(false);
        bomEnhancement.SetActive(false);
        speedEnhancement.SetActive(false);

        m_playerController.attaackRadius = 1;
        m_bombController.explodeRange = 1;
        m_playerController.speedCorrection = 1.0f;

        m_fade.FadeIn(2.0f);
    }

    // Update is called once per frame
    void Update()
    {
        // もしカウントが0となった時、画像を表示させる
        if (ShowCountdown.m_cuntZero == true && !hasFadedOut)
        {
            m_fade.FadeOut(2.0f);

            hasFadedOut = true;

            // 時間差処理をスタートさせる
            StartCoroutine(WaitAndShow());
        }
    }

    // 待機時間を作るための処理
    private IEnumerator WaitAndShow()
    {
        // 2秒間待機
        yield return new WaitForSeconds(2.0f);


        a.SetActive(true);
        miningEnhancement.SetActive(true);
        bomEnhancement.SetActive(true);
        speedEnhancement.SetActive(true);

        // 準備段階プレイヤー行動許可フラグをtrueにする
        m_readyPlayerAction = true;
    }

    public void OnRetryButton()
    {
        // フラグを元に戻す
        ShowCountdown.m_cuntZero = false;

        // タイマーをリセットして、すぐにスタートさせる
        m_gameTimer.OnReset();
        m_gameTimer.OnStart();

        // 画像を非表示にする
        a.SetActive(false);

        miningEnhancement.SetActive(false);
        bomEnhancement.SetActive(false);
        speedEnhancement.SetActive(false);

        hasFadedOut = false;

        m_fade.FadeIn(2.0f);

        // 準備段階プレイヤー行動許可フラグをfalseにする
        m_readyPlayerAction = false;

    }

    //採掘強化ボタンが押されたときの処理
    public void OnMiningEnhancementBottom()
    {

        //Lv1→Lv2への強化
        if (m_miningLevel == 1)
        {
            if (ScoreManagerSingleton.instance.m_score > 9)
            {
                m_playerController.digRange += 1.0f;//1.0→2.0fに増加
                m_levelManager.m_miningLevel++;
                ScoreManagerSingleton.instance.m_score -= 10;
                m_miningLevel++;
            }
        }
        //Lv2→Lv3への強化
        else if(m_miningLevel == 2)
        {
            if (ScoreManagerSingleton.instance.m_score > 99)
            {
                m_playerController.digRange += 0.5f;//2.0→2.5に増加
                m_levelManager.m_miningLevel++;
                ScoreManagerSingleton.instance.m_score -= 100;
                m_miningLevel++;
            }
        }

    }

    //爆弾強化ボタンが押されたときの処理
    public void OnBomEnhancementBottom()
    {

        //Lv1→Lv2への強化
        if (m_bomLevel == 1)
        {
            if (ScoreManagerSingleton.instance.m_score > 9)
            {
                m_bombController.explodeRange += 1.0f;//1.0→2.0fに増加
                m_levelManager.m_bomLevel++;
                ScoreManagerSingleton.instance.m_score -= 10;
                m_bomLevel++;
            }
        }
        //Lv2→Lv3への強化
        else if (m_bomLevel == 2)
        {
            if (ScoreManagerSingleton.instance.m_score > 99)
            {
                m_bombController.explodeRange += 1.0f;//2.0→3.0に増加
                m_levelManager.m_bomLevel++;
                ScoreManagerSingleton.instance.m_score -= 100;
                m_bomLevel++;
            }
        }
    }

    //移動強化ボタンが押されたときの処理
    public void OnSpeedEnhancementBottom()
    {
        //Lv1→Lv2への強化
        if (m_speedLevel == 1)
        {
            if (ScoreManagerSingleton.instance.m_score > 9)
            {
                m_playerController.speedCorrection += 1.0f;//1.0→2.0fに増加
                m_playerController.jumpCorrection += 1.0f;//ジャンプ力も増加
                m_levelManager.m_speedLevel++;
                ScoreManagerSingleton.instance.m_score -= 10;
                m_speedLevel++;
            }
        }
        //Lv2→Lv3への強化
        else if (m_speedLevel == 2)
        {
            if (ScoreManagerSingleton.instance.m_score > 99)
            {
                m_playerController.speedCorrection += 1.0f;//2.0→3.0に増加
                m_playerController.jumpCorrection += 1.0f;//ジャンプ力も増加
                m_levelManager.m_speedLevel++;
                ScoreManagerSingleton.instance.m_score -= 100;
                m_speedLevel++;
            }
        }

    }
}
