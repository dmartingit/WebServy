using System.IO;

namespace WebServy.Data;

public class AppState
{
    private static string ConfigFilepath => Path.Combine(Environment.CurrentDirectory, ".wsconfig");

    public Config Config { get; init; } = new(ConfigFilepath);
}
