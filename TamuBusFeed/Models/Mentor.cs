using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace TamuBusFeed.Models
{
    public class Mentor : Entity
    {
        public StaticBusInfo Static { get; set; }
        public Entity Driver { get; set; }
        public GpsData GPS { get; set; }
        public ApcData APC { get; set; }
        public RsaData RSA { get; set; }
        public Work CurrentWork { get; set; }
        public List<Stop> NextStops { get; set; }

        [JsonIgnore]
        public Guid Id => new(Key);
    }

    public class MentorStop : Entity
    {
        public string StopCode { get; set; }
        public DateTimeOffset EstimatedDepartTime { get; set; }
        public DateTimeOffset ScheduledDepartTime { get; set; }
        public DateTimeOffset ScheduledWorkDate { get; set; }
    }
}
