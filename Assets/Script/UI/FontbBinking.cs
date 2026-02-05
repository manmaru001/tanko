using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Font : MonoBehaviour
{
    public float speed = 1.0f;
    private float time;
    public TextMeshProUGUI text;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        text.color = GetTextColorAlpha(text.color);
    }

    //テキストのアルファ値をsinで変化させる
    Color GetTextColorAlpha(Color color)
    {
        //sinを元に0～1を往復する値を作成
        time += Time.deltaTime * speed * 5.0f;
        color.a = Mathf.Sin(time);

        return color;
    }

}
