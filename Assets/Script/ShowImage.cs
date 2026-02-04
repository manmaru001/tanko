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

    private bool hasFadedOut = false;

    void Start()
    {
        //m_scoreManager = FindFirstObjectByType<ScoreManager>();

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

    }

    public void OnMiningEnhancementBottom()
    {

        //if (m_scoreManager.m_score > 0)
        //{
        //    m_playerController.digRange += 1;
        //    m_scoreManager.m_score--;
        //}

        if(ScoreManagerSingleton.instance.m_score > 0)
        {
            m_playerController.digRange += 1;
            ScoreManagerSingleton.instance.m_score--;
        }

    }

    public void OnBomEnhancementBottom()
    {
        //if (m_scoreManager.m_score > 0)
        //{
        //    m_bombController.explodeRange += 2.0f;
        //    m_scoreManager.m_score--;
        //}

        if(ScoreManagerSingleton.instance.m_score > 0)
        {
            m_bombController.explodeRange += 2.0f;
            ScoreManagerSingleton.instance.m_score--;
        }
    }

    public void OnSpeedEnhancementBottom()
    {
        //if (m_scoreManager.m_score > 0)
        //{
        //    m_playerController.speedCorrection += 1.0f;
        //    m_scoreManager.m_score--;
        //}

        if(ScoreManagerSingleton.instance.m_score > 0)
        {
            m_playerController.speedCorrection += 1.0f;
            ScoreManagerSingleton.instance.m_score--;
        }

    }
}
