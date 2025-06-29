using System.Text.Json.Serialization;

public class GitHubContent
{
    public string Name { get; set; }

    [JsonPropertyName("download_url")]
    public string DownloadUrl { get; set; }
}
