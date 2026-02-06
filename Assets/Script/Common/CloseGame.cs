using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CloseGame : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        EndGame();
    }

    //ゲーム終了
    private void EndGame()
    {
        //ESCが押された時
        if(Input.GetKey(KeyCode.Escape))
        {
#if UNITY_EDITOR
                        //ゲームプレイ終了
                        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }

}
