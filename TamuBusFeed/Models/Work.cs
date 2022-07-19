using Newtonsoft.Json;

namespace TamuBusFeed.Models
{
    public class Work
    {
        public WorkPatternInfo Pattern { get; set; }
        public WorkRouteInfo Route { get; set; }
        public Entity Trip { get; set; }
    }

    public class WorkRouteInfo : Entity
    {
        public string RouteNumber { get; set; }
    }

    public class WorkPatternInfo : Entity
    {
        [JsonProperty("LongName")]
        public new string Name { get; set; }

        [JsonProperty("ShortName")]
        public string ShortName { get; set; }

        public string Destination { get; set; }
        public string DirectionKey { get; set; }
        public string DirectionName { get; set; }
    }
}
