using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

using ROTA.Loading;
using ROTA.Utils;

public class SplashScreen : MonoBehaviour
{

    private float m_logoDisplayTime = 3;
    private float m_logoFadeTime = 2;
    private RawImage m_logo;
    private Fade m_logoFade;
    private CoroutineTask m_preloadTask;
    private CoroutineTask m_logoDisplayTask;
    private bool m_displayTimeElapsed = false;

    private PreLoader m_loader;

    void Start()
    {
        m_loader = new PreLoader();

        // Begin pre load
        m_preloadTask = new CoroutineTask(m_loader.Load());
        m_logoDisplayTask = new CoroutineTask(WaitLogoDisplayTime(), false);

        m_logo = gameObject.transform.Find("Logo").gameObject.GetComponent<RawImage>();
        Color cur = m_logo.color;
        cur.a = 0;
        m_logo.color = cur;
        m_logoFade = m_logo.GetComponent<Fade>();
        m_logoFade.FadeInTimed(m_logoFadeTime, () => { m_logoDisplayTask.Start(); });
    }

    void Update()
    {
        if ( ! m_preloadTask.Running && m_displayTimeElapsed && m_logoFade.IsComplete())
        {
            EndSplash();
        }
    }

    void EndSplash()
    {
        SceneManager.LoadScene("MainLoad");
    }

    IEnumerator WaitLogoDisplayTime()
    {
        yield return new WaitForSeconds(m_logoDisplayTime);
        m_displayTimeElapsed = true;
    }

}