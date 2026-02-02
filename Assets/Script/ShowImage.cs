using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShowImage : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] private GameObject a;//GameObject型の変数aを宣言　好きなゲームオブジェクトをアタッチ
    void Start()
    {
        a.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (ShowCountdown.CountZero == true)
        a.SetActive(true);
    }
}
