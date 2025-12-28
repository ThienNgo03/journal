using static System.Net.WebRequestMethods;

namespace Core;
public class Config
{
    public string Url { get; set; }


    public Config(string url)
    {
        Url = url;
    }
}

