using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class UISoundFX : MonoBehaviour
{
    AudioSource m_audioSource;
    public AudioClip m_mainMenuClickSoundFX;

    void Awake() {
        m_audioSource = GetComponent<AudioSource>();
    }

    public void OnMainMenuClick()
    {
        Play(m_mainMenuClickSoundFX);
    }

    void Play(AudioClip clip)
    {
        if (clip != null)
        {
            m_audioSource.Stop();
            m_audioSource.clip = clip;
            m_audioSource.Play();
        }
    }
}
