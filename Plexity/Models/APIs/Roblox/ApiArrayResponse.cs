using System.Text.Json.Serialization;
namespace Plexity.Models.APIs.Roblox
{
    /// <summary>
    /// Roblox.Web.WebAPI.Models.ApiArrayResponse
    /// </summary>
    public class ApiArrayResponse<T>
    {
        [JsonPropertyName("data")]
        public IEnumerable<T> Data { get; set; } = null!;
    }
}
