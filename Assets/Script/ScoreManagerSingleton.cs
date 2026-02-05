using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScoreManagerSingleton : MonoBehaviour
{

    public static ScoreManagerSingleton instance; //インスタンス定義
    public int m_score = 0;//アクセスする変数

    void Start()
    {
        m_score = 0;
    }

    private void Awake()
    {
        if(instance == null)
        {
            //自身をインスタンスとする
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            //既に存在したら自身を削除
            Destroy(gameObject);
        }

    }


    // Update is called once per frame
    void Update()
    {
        
    }
}
