using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameContoroller : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //スコアが10を超えたらシーンをチェンジ
        if(ScoreManagerSingleton.instance.m_score >= 10000)
        {
            ChangeScene();
        }

        //デバッグ用Pキーでリザルトへ
        if(Input.GetKeyDown(KeyCode.P))
        {
            ChangeScene();
        }


    }


    private void ChangeScene()
    {
        //リザルトシーンの読み込み
        SceneManager.LoadScene("ResultScene");
    }


}
