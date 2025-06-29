using System.Text.Json.Serialization;

namespace Plexity.Models
{
    public class FontFamily
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = null!;

        [JsonPropertyName("faces")]
        public IEnumerable<FontFace> Faces { get; set; } = null!;
    }
}
