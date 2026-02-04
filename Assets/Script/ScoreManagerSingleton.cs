using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScoreManagerSingleton : MonoBehaviour
{

    public static ScoreManagerSingleton instance; //インスタンス定義
    public int m_score; //アクセスする変数

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
            //既に存在したら自信を削除
            Destroy(gameObject);
        }

    }


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
