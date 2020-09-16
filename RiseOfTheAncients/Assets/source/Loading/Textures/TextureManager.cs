using System.Collections.Generic;
using UnityEngine;

public static class TextureManager
{
    static Dictionary<string, Texture2D> m_textures = new Dictionary<string, Texture2D>();
    static List<Texture2D> m_backgrounds = null;

    public static void Add(string id, Texture2D texture)
    {
        m_textures.Add(id, texture);
    }

    public static Texture2D Get(string id)
    {
        if (m_textures.ContainsKey(id))
        {
            return m_textures[id];
        }
        else
        {
            Debug.Log("Texture ID: " + id + " not found.");
            return null;
        }
    }

    /// <summary>
    /// Defines the backgrounds list to be used in MainLoad screen and MainMenu. Should only be called once.
    /// </summary>
    public static void SetBackgrounds(List<Texture2D> backgrounds)
    {
        if (m_backgrounds == null)
        {
            m_backgrounds = backgrounds;
        }
        else
        {
            Debug.LogError("InvalidOperation: SpriteManager.SetBackgrounds should only be called once!");
        }
    }

    /// <summary>
    /// Get the backgrounds list.
    /// </summary>
    public static List<Texture2D> GetBackgrounds()
    {
        if (m_backgrounds == null)
        {
            Debug.LogError("InvalidOperation: SpriteManager.GetBackgrounds should only be called after setting the backgrounds!");
        }

        return m_backgrounds;
    }
}