using System.IO;

namespace ROTA.Loading
{

public static class RootPath
{

    public static string Value
    { 
        get
        {
            if (m_rootPath == null)
            {
                m_rootPath = Directory.GetCurrentDirectory();
            }

            return m_rootPath;
        }
    }

    public static string Data
    {
        get
        {
            if (m_dataPath == null)
            {
                m_dataPath = Path.Combine(Value, "data"); 
            }

            return m_dataPath;
        }
    }

    private static string m_rootPath = null;
    private static string m_dataPath = null;

}

}
