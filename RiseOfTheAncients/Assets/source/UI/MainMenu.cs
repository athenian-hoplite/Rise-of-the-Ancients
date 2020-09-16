using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    static readonly float SLIDE_FADE_TIME = 2;
    static readonly float SLIDE_DURATION = 5;

    RawImage m_slideTarget;
    Slideshow m_slideshow;

    void Start()
    {
        BuildMainMenuUI();
    }

    void BuildMainMenuUI()
    {
        // Setup slideshow
        m_slideTarget = GameObject.Find("SlideTarget").GetComponent<RawImage>();
        m_slideshow = new Slideshow(m_slideTarget, TextureManager.GetBackgrounds(), true);
        m_slideshow.FadeTime = SLIDE_FADE_TIME;
        m_slideshow.AutoPlay(SLIDE_DURATION);

        // Main Menu
        GameObject.Find("MenuPanel").GetComponent<RawImage>().texture = TextureManager.Get("main_menu_panel");
    }

    public void MapEditorClicked()
    {
        SceneManager.LoadScene("MapEditor");
    }

}