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

    Color GetTextColorAlpha(Color color)
    {
        //sin‚ğŒ³‚É0`1‚ğ‰•œ‚·‚é’l‚ğì¬
        time += Time.deltaTime * speed * 5.0f;
        color.a = Mathf.Sin(time);

        return color;
    }

}
