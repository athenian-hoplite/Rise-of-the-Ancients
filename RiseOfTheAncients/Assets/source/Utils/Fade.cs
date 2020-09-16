using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class Fade : MonoBehaviour {

    private RawImage m_image;
    private float m_targetAlpha;
    private Action m_onCompleteCallback;

    private float m_fadeRate; // Used when only a fade rate is specified
    private bool m_timed; // Used to flag if fading is timed or not
    private float m_duration; // Duration of the fade if timed
    private float m_elapsed; // Elapsed time since timed fade initiated

    void Awake() 
    {
        m_image = this.GetComponent<RawImage>();
        this.enabled = false;
    }
    
    void Update () 
    {
        DoFade();
    }
 
    public void FadeOut(float fadeRate = 1, Action callback = null)
    {
        m_timed = false;
        m_targetAlpha = 0.0f;
        m_fadeRate = fadeRate;
        m_onCompleteCallback = callback;
        this.enabled = true;
    }
 
    public void FadeIn(float fadeRate = 1, Action callback = null)
    {
        m_timed = false;
        m_targetAlpha = 1.0f;
        m_fadeRate = fadeRate;
        m_onCompleteCallback = callback;
        this.enabled = true;
    }

    public void FadeInTimed(float duration = 1, Action callback = null)
    {
        m_timed = true;
        m_targetAlpha = 1.0f;
        m_elapsed = 0;
        m_duration = duration;
        m_onCompleteCallback = callback;
        this.enabled = true;
    }

    public void FadeOutTimed(float duration = 1, Action callback = null)
    {
        m_timed = true;
        m_targetAlpha = 0f;
        m_elapsed = 0;
        m_duration = duration;
        m_onCompleteCallback = callback;
        this.enabled = true;
    }

    public bool IsComplete()
    {
        return m_targetAlpha == m_image.color.a;
    }

    void DoFade()
    {
        Color curColor = m_image.color;
        float alphaDiff = Mathf.Abs(curColor.a - m_targetAlpha);
        if (alphaDiff > 0.01f)
        {
            float interpolator;
            if (m_timed) 
            {
                m_elapsed += Time.deltaTime;
                interpolator = m_elapsed / m_duration;
            }
            else
            {
                interpolator = m_fadeRate * Time.deltaTime;
            }
            curColor.a = Mathf.Lerp(curColor.a, m_targetAlpha, interpolator);
            m_image.color = curColor;
        }
        else
        {
            EndFade();
        }
    }

    void EndFade()
    {
        Color curColor = m_image.color;
        curColor.a = m_targetAlpha;
        m_image.color = curColor;
        if (m_onCompleteCallback != null) m_onCompleteCallback();
        this.enabled = false;
    }
}