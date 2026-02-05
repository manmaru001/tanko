using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ResultSceneContoroller : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //左クリックを押したときにシーン移行
        if(Input.GetMouseButtonDown(0))
        { 
            ChangeScene();
        }
    }

    void ChangeScene()
    {
        //移動先のシーンの読み込み(タイトルシーン)
        SceneManager.LoadScene("TitleScene");
    }

}
