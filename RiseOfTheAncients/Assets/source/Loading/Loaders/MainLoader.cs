using System.Collections;
using System.IO;
using UnityEngine;

namespace ROTA.Loading
{

/// <summary>
/// Responsible for the main loading sequence on game start.
/// </summary>
public class MainLoader : ALoader
{
    
    private static readonly string MAIN_MENU_UI_PATH = Path.Combine(RootPath.Data, "gfx", "UI", "main_menu");
    
    /// <summary>
    /// Main loading sequence on game start.
    /// </summary>
    public override IEnumerator Load()
    {
        yield return LoadMainMenuUI();
        ProgressTo(1);
    }

    /// <summary>
    /// Loads the Main Menu UI assets.
    /// </summary>
    private IEnumerator LoadMainMenuUI()
    {
        string[] extensions = { ".png" };
        yield return TextureLoader.LoadTexturesFromDir(MAIN_MENU_UI_PATH, extensions);
    }

}

}