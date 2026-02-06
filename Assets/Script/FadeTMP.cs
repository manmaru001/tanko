using System.Collections;
using System;
using UnityEngine;
using TMPro;

public class FadeTMP : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI m_text = null;

    private void Reset()
    {
        m_text = GetComponent<TextMeshProUGUI>();
    }

    public void FadeIn(float duration, Action on_completed = null)
    {
        StartCoroutine(ChangeAlphaValue(duration, 0f, 1f, on_completed));
    }

    public void FadeOut(float duration, Action on_completed = null)
    {
        StartCoroutine(ChangeAlphaValue(duration, 1f, 0f, on_completed));
    }

    private IEnumerator ChangeAlphaValue(float duration, float startAlpha, float endAlpha, Action on_completed)
    {
        float time = 0.0f;
        Color color = m_text.color;

        while (time < duration)
        {
            time += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, endAlpha, time / duration);

            // 色のアルファ値だけ変える
            color.a = alpha;
            m_text.color = color;

            yield return null;
        }

        color.a = endAlpha;
        m_text.color = color;

        if (on_completed != null) on_completed();
    }
}