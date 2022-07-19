namespace TamuBusFeed.Models
{
    public class StaticBusInfo
    {
        public Agency Agency { get; set; }
        public string Color { get; set; }

        /// <summary>
        /// The type of bus, typically "GILLIG" for the manufacturer.
        /// </summary>
        public string Type { get; set; }
    }
}
