using System.ComponentModel.DataAnnotations;

namespace WebServy.Data;

public sealed record WebService
{
    [Required]
    public string Name { get; set; } = string.Empty;
    [Required]
    public string Url { get; set; } = string.Empty;
    public string Uuid { get; set; } = Guid.NewGuid().ToString();
}
