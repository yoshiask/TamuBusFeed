using Newtonsoft.Json;
using System;

namespace TamuBusFeed.Models
{
    public class GpsData
    {
        public DateTimeOffset Date { get; set; }
        public double Dir { get; set; }
        public double Lat { get; set; }
        public double Long { get; set; }

        [JsonProperty("Spd")]
        public double Speed { get; set; }
    }
}
