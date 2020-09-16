using System.Collections;
using System.Collections.Generic;
using System.IO;
using ROTA.Utils;
using UnityEngine;

/// <summary>
/// Provides utility functions for loading textures from the filesystem.
/// </summary>
public static class TextureLoader
{   
    /// <summary>
    /// Create a Texture2D from file at the given path. If file at given path is
    /// an invalid format BadImageFormatException is thrown.
    /// </summary>
    public static Texture2D LoadTexture(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        Texture2D texture = new Texture2D(0,0);
        if ( ! texture.LoadImage(bytes))
        {
            throw new System.BadImageFormatException();
        }
        else
        {
            return texture;
        }
    }

    /// <summary>
    /// Loads all textures present in the given directory path that match the provided extensions array. 
    /// The loaded textures are added to the TextureManager.
    /// </summary>
    public static IEnumerator LoadTexturesFromDir(string path, string[] extensions)
    {
        DirectoryInfo dirInfo = new DirectoryInfo(path);
        IEnumerable<FileInfo> files = dirInfo.GetFilesByExtensions(extensions);

        foreach(FileInfo file in files)
        {
            yield return null;
            string filePath = Path.Combine(file.DirectoryName, file.Name);
            try
            {
                Texture2D texture = TextureLoader.LoadTexture(filePath);
                TextureManager.Add(Path.GetFileNameWithoutExtension(filePath), texture);
            }
            catch(System.BadImageFormatException)
            {
                Debug.Log("Error: Image " + file.Name + " has an invalid format.");
            }
        }
    }

}