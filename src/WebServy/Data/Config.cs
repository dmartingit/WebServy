using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WebServy.Data;

public sealed class Config
{
    private readonly string filepath;

    public Config(string filepath)
    {
        this.filepath = filepath;

        Load();
        WebServices.CollectionChanged += (_, _) => Save();
        LastUsedWebServiceUuid.Changed += (_, _) => Save();
    }

    public ObservableCollection<WebService> WebServices { get; set; } = new();
    public Observable<string> LastUsedWebServiceUuid { get; set; } = new();

    public void Load()
    {
        if (File.Exists(filepath))
        {
            using StreamReader reader = new(File.OpenRead(filepath));
            string json = reader.ReadToEnd();
            Data data = JsonSerializer.Deserialize(json, ConfigJsonContext.Default.Data)!;

            WebServices.Clear();
            for (int i = 0; i < data.WebServices.Length; ++i)
            {
                WebServices.Add(data.WebServices[i]);
            }
            LastUsedWebServiceUuid.Value = data.LastUsedWebServiceUuid;
        }
    }

    public void Save()
    {
        string json = JsonSerializer.Serialize(new Data
        {
            WebServices = WebServices.ToArray(),
            LastUsedWebServiceUuid = LastUsedWebServiceUuid.Value
        });
        File.WriteAllText(filepath, json);
    }

    internal sealed class Data
    {
        public WebService[] WebServices { get; set; } = Array.Empty<WebService>();
        public string? LastUsedWebServiceUuid { get; set; }
    }
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(Config.Data))]
[JsonSerializable(typeof(WebService[]))]
[JsonSerializable(typeof(WebService))]
internal partial class ConfigJsonContext : JsonSerializerContext { }
