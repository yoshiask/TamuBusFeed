using System;
using System.Collections.Generic;
using System.Linq;

namespace TamuBusFeed.Models
{
    public class TimeTable
    {
        public string Destination { get; set; }

        public List<TimeStop> TimeStops { get; } = new();

        /// <summary>
        /// Gets the leave times for the specified trip.
        /// </summary>
        /// <returns>
        /// The specified row of the time table.
        /// </returns>
        public List<TimeCell> GetTrip(int tripIdx)
        {
            List<TimeCell> cells = new(TimeStops.Count);
            foreach (var timeStop in TimeStops)
                cells.Add(new(timeStop, timeStop.LeaveTimes[tripIdx]));

            return cells;
        }

        /// <summary>
        /// Gets the next trip for the specified stop.
        /// </summary>
        /// <param name="stopIdx">
        /// The index of the stop to check.
        /// </param>
        /// <param name="targetTime">
        /// The target time to match against. Usually the current time.
        /// </param>
        /// <returns>
        /// The row of the time table that contains the next leave time.
        /// </returns>
        public List<TimeCell> GetNextTrip(int stopIdx, DateTimeOffset targetTime)
        {
            var stop = TimeStops[stopIdx];
            var time = stop.LeaveTimes.FirstOrDefault(lt => lt.HasValue && lt.Value >= targetTime);

            int tripIdx = stop.LeaveTimes.IndexOf(time);
            return GetTrip(tripIdx);
        }

        /// <summary>
        /// Gets all the trips (rows) of this time table.
        /// </summary>
        /// <returns>
        /// This time table in row-major form.
        /// </returns>
        public List<List<TimeCell>> GetTrips()
        {
            List<List<TimeCell>> trips = new(TimeStops.FirstOrDefault()?.LeaveTimes.Count ?? 0);
            for (int i = 0; i < trips.Capacity; i++)
            {
                trips.Add(GetTrip(i));
            }
            return trips;
        }
    }

    /// <summary>
    /// Represents a column in a <see cref="TimeTable"/>.
    /// </summary>
    public class TimeStop
    {
        public List<DateTimeOffset?> LeaveTimes { get; } = new();

        public string Name { get; set; }

        public IEnumerable<string> GetFormattedLeaveTimes() => LeaveTimes.Select(t => t.HasValue ? t.Value.ToString("hh:mm tt") : "...");
    }

    /// <summary>
    /// Represents a cell (specific stop in a given trip) in a <see cref="TimeStop"/>.
    /// </summary>
    public class TimeCell
    {
        public TimeStop TimeStop { get; set; }
        public DateTimeOffset? LeaveTime { get; set; }

        public TimeCell(TimeStop timeStop, DateTimeOffset? leaveTime)
        {
            TimeStop = timeStop;
            LeaveTime = leaveTime;
        }
    }
}
