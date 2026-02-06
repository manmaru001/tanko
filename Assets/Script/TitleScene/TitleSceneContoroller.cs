using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleSceneContoroller : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //左クリックでシーン移行
        if(Input.GetMouseButtonDown(0))
        {
            ChangeScene1();
        }
        if(Input.GetMouseButtonDown(1))
        {
            ChangeScene2();
        }

    }

    //シーン移行処理
    void ChangeScene1()
    {
        //移動先のシーンの読み込み(サンプルシーン)
        SceneManager.LoadScene("SampleScene");
       
        
    }

    void ChangeScene2()
    {
        SceneManager.LoadScene("GameRuleScene");
    }

}
