
using System.Collections.Generic;
using System.IO;
using System.Linq;

public static class DirectoryInfoExtensions
{
    /// <summary>
    /// Extension method for getting all FileInfo in a directory that match the given file extensions.
    /// </summary>
    /// <param name="extensions">Extensions to look for.</param>
    public static IEnumerable<FileInfo> GetFilesByExtensions(this DirectoryInfo dir, params string[] extensions)
    {
        IEnumerable<FileInfo> files = dir.EnumerateFiles();
        return files.Where(f => extensions.Contains(f.Extension));
    }
}