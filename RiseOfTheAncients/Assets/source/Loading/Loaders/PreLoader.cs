using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ROTA.Loading
{

/// <summary>
/// Responsible for loading the assets needed for the main loading screen.
/// </summary>
public class PreLoader : ALoader
{

    /// <summary>
    /// Loads the necessary assets used for the main load screen.
    /// </summary>
    public override IEnumerator Load()
    {
        yield return LoadUI();
        yield return LoadBackgrounds();
    }

    /// <summary>
    /// Loads the UI assets needed for the MainLoad UI.
    /// </summary>
    private IEnumerator LoadUI()
    {
        // Load MainLoadUI assets
        string dirPath = Path.Combine(RootPath.Data, "gfx", "UI", "loading");
        string[] extensions = { ".png" };
        
        yield return TextureLoader.LoadTexturesFromDir(dirPath, extensions);
    }

    /// <summary>
    /// Loads the backgrounds used in the MainLoadUI slideshow.
    /// </summary>
    private IEnumerator LoadBackgrounds()
    {
        // Load backgrounds
        DirectoryInfo backgroundsDir = new DirectoryInfo(Path.Combine(RootPath.Data, "gfx", "UI", "loading", "backgrounds"));
        IEnumerable<FileInfo> backgroundFiles = backgroundsDir.GetFilesByExtensions(".png", ".jpg");

        List<Texture2D> backgrounds = new List<Texture2D>();
        foreach(FileInfo file in backgroundFiles)
        {
            yield return null;
            string path = Path.Combine(file.DirectoryName, file.Name);
            try
            {
                Texture2D texture = TextureLoader.LoadTexture(path);
                backgrounds.Add(texture);
            }
            catch(System.BadImageFormatException)
            {
                Debug.Log("Error: Image " + file.Name + " has an invalid format.");
            }
        }

        TextureManager.SetBackgrounds(backgrounds);
    }

}

}