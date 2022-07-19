using Newtonsoft.Json;

namespace TamuBusFeed.Models
{
    public class GisValues
    {
        [JsonProperty("mapSr")]
        public string MapSpatialReference { get; set; }

        [JsonProperty("mapUrl")]
        public string MapBasemapUrl { get; set; }

        [JsonProperty("geometryUrl")]
        public string GeometryUrl { get; set; }
    }
}
