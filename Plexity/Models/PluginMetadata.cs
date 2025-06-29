using System.Text.Json.Serialization;

public class PluginMetadata
{
    public string Name { get; set; }
    public string Description { get; set; }

    [JsonPropertyName("icon_url")]
    public string IconUrl { get; set; }

    public List<string> Code { get; set; }
}
