using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SocialPlatforms.Impl;
public class LevelManager : MonoBehaviour
{
    public TextMeshProUGUI miningDisplay;
    public TextMeshProUGUI bomDisplay;
    public TextMeshProUGUI speedDisplay;
    public float m_miningLevel = 1;
    public float m_bomLevel = 1;
    public float m_speedLevel = 1;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        miningDisplay.text = m_miningLevel.ToString();
        bomDisplay.text = m_bomLevel.ToString();
        speedDisplay.text = m_speedLevel.ToString();
    }
}
