using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

using ROTA.Loading;
using ROTA.Utils;

public class MainLoad : MonoBehaviour
{
    RawImage m_slidesTarget;
    Slideshow m_slideshow;
    MainLoader m_loader;

    void Start()
    {
        BuildMainLoadUI();

        m_loader = new MainLoader();
        m_loader.OnProgress = OnLoadProgress;
        m_loader.OnComplete = LoadEnded;

        new CoroutineTask(m_loader.Load()); // TaskManager execute
    }

    void LoadEnded()
    {
        SceneManager.LoadScene("MainMenu");
    }

    void OnLoadProgress(float progress)
    {
        if (progress == 1) return;

        m_slideshow.Next();
    }

    void BuildMainLoadUI()
    {
        m_slidesTarget = GameObject.Find("SlidesTarget").GetComponent<RawImage>();
        m_slideshow = new Slideshow(m_slidesTarget, TextureManager.GetBackgrounds());
        m_slideshow.Start();
    }

}