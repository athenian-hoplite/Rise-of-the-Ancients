using System;
using System.Collections;
using UnityEngine;

namespace ROTA.Loading
{

/// <summary>
/// Base class of resource loaders providing a unified entry point and basic progress tracking as well as
/// OnProgress and OnComplete callbacks. 
/// </summary>
public abstract class ALoader
{
    
    /// <summary>
    /// Loading progress in the [0,1] range.
    /// </summary>
    public float Progress { get { return m_progress; } }

    /// <summary>
    /// Function called everytime there is loading progress. Receives float argument with current progress value.
    /// </summary>
    public Action<float> OnProgress { set { m_onProgress = value; } }

    /// <summary>
    /// Function called when progress reaches 1 and loading is complete.
    /// </summary>
    public Action OnComplete { set { m_onComplete = value; } }

    protected float m_progress = 0;
    protected Action<float> m_onProgress = null;
    protected Action m_onComplete = null;

    /// <summary>
    /// Begins loading operation.
    /// </summary>
    public abstract IEnumerator Load();

    /// <summary>
    /// Define the new progress level. Provided value is clamped to [0,1] range.
    /// If the OnProgress callback is defined it is called.
    /// If progress level is 1 and the OnComplete callback is defined it is called.
    /// </summary>
    protected void ProgressTo(float newProgress)
    {
        m_progress = Mathf.Clamp01(newProgress);
        if (m_onProgress != null)
        {
            m_onProgress(m_progress);
        }
        if (m_progress == 1 && m_onComplete != null)
        {
            m_onComplete();
        }
    }

}

}