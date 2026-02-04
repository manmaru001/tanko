using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class JewelryController : MonoBehaviour
{

    ScoreManager m_scoreManager;
    // Start is called before the first frame update
    void Start()
    {
        m_scoreManager = FindFirstObjectByType<ScoreManager>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    //プレイヤータグと衝突したらオブジェクトを破壊
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Destroy(gameObject);
            m_scoreManager.m_score++;
        }
    }

}
