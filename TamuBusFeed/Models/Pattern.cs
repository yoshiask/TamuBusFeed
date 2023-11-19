using System.Collections.Generic;
using System.Linq;

namespace TamuBusFeed.Models;

public class Pattern
{
    public string RouteKey { get; set; }

    public string? Color { get; set; }

    public List<PatternPath> PatternPaths { get; set; }

    public IEnumerable<PatternPoint> Points()
    {
        foreach (var path in PatternPaths)
            foreach (var point in path.PatternPoints)
                yield return point;
    }

    public IEnumerable<PatternPoint> StopPoints() => Points().Where(p => p.IsStop);
}

public class PatternPath
{
    public string PatternKey { get; set; }

    public List<PatternPoint> PatternPoints { get; set; }
}

public class PatternPoint
{
    public string Key { get; set; }

    public string Name { get; set; }

    public string Description { get; set; }

    public int Rank { get; set; }

    public double Longitude { get; set; }

    public double Latitude { get; set; }

    public bool IsStop { get; set; }

    public bool IsTimePoint { get; set; }

    public Stop? Stop { get; set; }

    public int RouteHeaderRank { get; set; }

    public double DistanceToPreviousPoint { get; set; }
}
