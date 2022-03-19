using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace TamuBusFeed.Models
{
    public class TimeTable : ObservableObject
    {
        private ObservableCollection<TimeStop> timeStops;
        public ObservableCollection<TimeStop> TimeStops
        {
            get => timeStops;
            set => SetProperty(ref timeStops, value);
        }

        /// <summary>
        /// Gets the leave times for the specified trip.
        /// </summary>
        /// <returns>
        /// The times in the specified row of the time table.
        /// </returns>
        public List<DateTimeOffset?> GetLeaveTimes(int tripIdx)
        {
            List<DateTimeOffset?> leaveTimes = new(TimeStops.Count);
            foreach (var timeStop in TimeStops)
                leaveTimes.Add(timeStop.LeaveTimes[tripIdx]);
            return leaveTimes;
        }

        /// <summary>
        /// Gets the next leave time for the specified stop.
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
        public List<DateTimeOffset?> GetNearestLeaveTimes(int stopIdx, DateTimeOffset targetTime)
        {
            var stop = TimeStops[stopIdx];
            var time = stop.LeaveTimes.FirstOrDefault(lt => lt.HasValue && lt.Value >= targetTime);

            int tripIdx = stop.LeaveTimes.IndexOf(time);
            return GetLeaveTimes(tripIdx);
        }
    }

    public class TimeStop : ObservableObject
    {
        private ObservableCollection<DateTimeOffset?> leaveTimes;
        public ObservableCollection<DateTimeOffset?> LeaveTimes
        {
            get => leaveTimes;
            set => SetProperty(ref leaveTimes, value);
        }

        private string name;
        public string Name
        {
            get => name;
            set => SetProperty(ref name, value);
        }

        public IEnumerable<string> GetFormattedLeaveTimes() => leaveTimes.Select(t => t.HasValue ? t.Value.ToString("hh:mm tt") : "...");
    }
}
