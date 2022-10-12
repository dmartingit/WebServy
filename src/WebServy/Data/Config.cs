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
        LastUsedWebServiceUuid.Changed += (_, _) => Save();
        UseIconNavBar.Changed += (_, _) => Save();
        WebServices.CollectionChanged += (_, _) => Save();
        WindowPlacement.Changed += (_, _) => Save();
    }

    public Observable<string> LastUsedWebServiceUuid { get; set; } = new();
    public Observable<bool> UseIconNavBar { get; set;} = new();
    public ObservableCollection<WebService> WebServices { get; set; } = new();
    public Observable<WindowPlacement> WindowPlacement { get; set; } = new(new());

    public void Load()
    {
        if (File.Exists(filepath))
        {
            using StreamReader reader = new(File.OpenRead(filepath));
            string json = reader.ReadToEnd();
            Data data = JsonSerializer.Deserialize<Data>(json)!;

            LastUsedWebServiceUuid.Value = data.LastUsedWebServiceUuid;
            UseIconNavBar.Value = data.UseIconNavBar ?? false;
            WebServices.Clear();
            for (int i = 0; i < data.WebServices.Length; ++i)
            {
                WebServices.Add(data.WebServices[i]);
            }
            
            if (data.WindowPlacement is not null) WindowPlacement.Value = data.WindowPlacement;
        }
    }

    public void Save()
    {
        string json = JsonSerializer.Serialize(new Data
        {
            LastUsedWebServiceUuid = LastUsedWebServiceUuid.Value,
            UseIconNavBar = UseIconNavBar.Value,
            WebServices = WebServices.ToArray(),
            WindowPlacement = WindowPlacement.Value
        });
        File.WriteAllText(filepath, json);
    }

    internal sealed class Data
    {
        public string? LastUsedWebServiceUuid { get; set; }
        public bool? UseIconNavBar { get; set; }
        public WebService[] WebServices { get; set; } = Array.Empty<WebService>();
        public WindowPlacement? WindowPlacement { get; set; } = new();
    }
}
