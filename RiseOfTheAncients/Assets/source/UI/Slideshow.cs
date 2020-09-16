using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using ROTA.Utils;
using System.Collections;

public class Slideshow
{
    /// <summary>
    /// If the slideshow was created with fade transition set to true, FadeTime indicates
    /// the duration, in seconds, of the fade effect when fading in/out from one slide to another.
    /// </summary>
    public float FadeTime { get { return m_fadeTime; } set { m_fadeTime = value; } }

    RawImage[] m_slidesTarget;
    Fade[] m_fadeComponents = null;
    List<Texture2D> m_slides;
    int m_curIndex = 0;
    CoroutineTask m_autoPlayTask = null;
    bool m_fadeTransition;
    int m_canvasIndex = 0;
    float m_fadeTime = 1;

    public Slideshow(RawImage slidesTarget, List<Texture2D> slides, bool fadeTransition = false)
    {
        m_fadeTransition = fadeTransition;
        if (m_fadeTransition)
        {
            // Duplicate canvas, this makes a fade transition between slides easier. 
            // One canvas fades out with the old slide while the other canvas fades in with the new slide
            m_slidesTarget = new RawImage[2];
            m_slidesTarget[0] = slidesTarget;
            m_slidesTarget[1] = Object.Instantiate(slidesTarget, slidesTarget.gameObject.transform.parent);
            // Put canvas clone behind the original
            m_slidesTarget[1].gameObject.transform.SetSiblingIndex(m_slidesTarget[0].gameObject.transform.GetSiblingIndex());

            // Cache Fade components
            m_fadeComponents = new Fade[2];
            m_fadeComponents[0] = m_slidesTarget[0].gameObject.AddComponent<Fade>();
            m_fadeComponents[1] = m_slidesTarget[1].gameObject.AddComponent<Fade>();
        }
        else
        {
            m_slidesTarget = new RawImage[1];
            m_slidesTarget[0] = slidesTarget;
        }
        m_slides = slides;
    }

    public void Start()
    {
        m_curIndex = 0;
        m_canvasIndex = 0;
        m_slidesTarget[0].texture = m_slides[0];
    }

    public void Stop()
    {
        if (m_autoPlayTask != null)
        {
            m_autoPlayTask.Stop();
            m_autoPlayTask = null;
        } 
    }

    public void AutoPlay(float slideDuration)
    {
        m_autoPlayTask = new CoroutineTask(__AutoPlay(slideDuration));
    }

    public void Next()
    {
        m_curIndex++;
        if (m_curIndex >= m_slides.Count) m_curIndex = 0;

        if (m_fadeTransition)
        {
            m_fadeComponents[m_canvasIndex].FadeOutTimed(m_fadeTime);
            m_canvasIndex++;
            if (m_canvasIndex >= m_slidesTarget.Length) m_canvasIndex = 0;
            m_slidesTarget[m_canvasIndex].texture = m_slides[m_curIndex];
            m_fadeComponents[m_canvasIndex].FadeInTimed(m_fadeTime);
        }
        else
        {
            m_slidesTarget[0].texture = m_slides[m_curIndex];
        }
    }

    public void Previous()
    {
        m_curIndex--;
        if (m_curIndex < 0) m_curIndex = m_slides.Count - 1;

        if (m_fadeTransition)
        {
            m_fadeComponents[m_canvasIndex].FadeOutTimed(m_fadeTime);
            m_canvasIndex--;
            if (m_canvasIndex < 0) m_canvasIndex = 1;
            m_slidesTarget[m_canvasIndex].texture = m_slides[m_curIndex];
            m_fadeComponents[m_canvasIndex].FadeInTimed(m_fadeTime);
        }
        else
        {
            m_slidesTarget[0].texture = m_slides[m_curIndex];
        }
    }

    IEnumerator __AutoPlay(float slideDuration)
    {
        Start();
        yield return new WaitForSeconds(slideDuration);
        while (true)
        {
            Next();
            yield return new WaitForSeconds(slideDuration);
        }
    }

}