using UnityEngine;
using UnityEditor;
using System.Collections;
using UnityEditor.SceneManagement;
using System.IO;

class EditorScrips : EditorWindow
{

    [MenuItem("Play/Execute starting scene _%h")]
    public static void RunMainScene()
    {
        // If current scene is not splash save scene path to temp file
        if ( ! EditorSceneManager.GetActiveScene().name.Equals("SplashScreen"))
        {
            File.WriteAllText(".tempScenePlayScript", "Assets/scenes/" + EditorSceneManager.GetActiveScene().name + ".unity");
        }

        // Save open scenes before changing to splash screen
        EditorSceneManager.SaveOpenScenes(); 

        // Change to splash screen and play
        EditorSceneManager.OpenScene("Assets/scenes/SplashScreen.unity");
        EditorApplication.isPlaying=true;
    }
   
    [MenuItem("Play/Reload last edited scene _%g")]
    public static void ReturnToLastScene()
    {
        // Read last scene path from temp file and open that scene
        EditorSceneManager.OpenScene(File.ReadAllText(".tempScenePlayScript"));
    }
    
}