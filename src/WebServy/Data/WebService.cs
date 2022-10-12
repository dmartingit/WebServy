using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace WebServy.Data;

public sealed record WebService
{
    public Observable<bool> Enabled { get; set; } = new(true);
    public string IconUrl { get; set; } = string.Empty;
    [Required]
    public string Name { get; set; } = string.Empty;
    [JsonIgnore()]
    public Observable<int> UnreadMessagesCount { get; set; } = new();
    [Required]
    public string Url { get; set; } = string.Empty;
    [Required]
    public string Uuid { get; set; } = Guid.NewGuid().ToString();
}
