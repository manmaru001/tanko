using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DayManager : MonoBehaviour
{
    public TextMeshProUGUI dayDisplay;
    public int m_dayCount;
    // Start is called before the first frame update
    void Start()
    {
        m_dayCount = 1;
    }

    // Update is called once per frame
    void Update()
    {
        dayDisplay.text =  "Day " + m_dayCount.ToString();
    }
}
