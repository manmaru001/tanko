using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ScoreResultScene : MonoBehaviour
{

    public TextMeshProUGUI ScoreText;
    // Start is called before the first frame update
    void Start()
    {
        ScoreText.text = "Score:" + ScoreManagerSingleton.instance.m_score;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
