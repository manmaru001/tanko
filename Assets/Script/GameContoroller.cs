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
        if(ScoreManagerSingleton.instance.m_score >= 10)
        {
            ChangeScene();
        }

    }


    private void ChangeScene()
    {
        SceneManager.LoadScene("ResultScene");
    }


}
