using System;
using System.Collections.Generic;
using System.Linq;
using Restory.Gameplay.TimeSystems;
using Random = UnityEngine.Random;

namespace ReStoryMoreWork;

/// <summary>
/// The in-game hours, within today's game day, between ArrivalStartHour and ArrivalEndHour. A game day runs from
/// the morning start (8:00) to the next morning, so a window like 6 to 10 means 8:00-10:00 this morning plus
/// 6:00-8:00 tomorrow before the shop opens, and 0 to 24 means the whole day.
/// </summary>
internal readonly struct ArrivalWindow
{
    private static readonly TimeSpan MinimumDelay = TimeSpan.FromMinutes(1);

    private readonly List<(DateTime from, DateTime to)> _segments;

    private ArrivalWindow(List<(DateTime from, DateTime to)> segments)
    {
        _segments = segments;
    }

    internal static ArrivalWindow ForToday(GameCalendar calendar)
    {
        DateTime dayStart = calendar.CurrentDayStartTime;
        int startHour = Settings.ArrivalStartHour.Value;
        int endHour = Settings.ArrivalEndHour.Value;

        int lengthHours = Modulo24(endHour - startHour);

        if (lengthHours == 0)
        {
            lengthHours = 24;
        }

        int startPosition = Modulo24(startHour - dayStart.Hour);
        List<(DateTime, DateTime)> segments = new List<(DateTime, DateTime)>();

        if (startPosition + lengthHours <= 24)
        {
            segments.Add((dayStart.AddHours(startPosition), dayStart.AddHours(startPosition + lengthHours)));
        }
        else
        {
            segments.Add((dayStart, dayStart.AddHours(startPosition + lengthHours - 24)));
            segments.Add((dayStart.AddHours(startPosition), dayStart.AddHours(24)));
        }

        return new ArrivalWindow(segments);
    }

    /// <summary>A random moment in the window that is at least a minute from now; right away if it is over.</summary>
    internal DateTime RandomTime(DateTime now)
    {
        DateTime earliest = now + MinimumDelay;

        List<(DateTime from, DateTime to)> open = _segments
            .Where(segment => segment.to > earliest)
            .Select(segment => (segment.from > earliest ? segment.from : earliest, segment.to))
            .ToList();

        if (open.Count == 0)
            return earliest;

        double totalSeconds = open.Sum(segment => (segment.to - segment.from).TotalSeconds);
        double pick = Random.Range(0f, (float)totalSeconds);

        foreach ((DateTime from, DateTime to) in open)
        {
            double length = (to - from).TotalSeconds;

            if (pick <= length)
                return from + TimeSpan.FromSeconds(pick);

            pick -= length;
        }

        return open[open.Count - 1].to;
    }

    public override string ToString()
    {
        return string.Join(" and ", _segments.Select(segment => $"{segment.from:HH:mm}-{segment.to:HH:mm}"));
    }

    private static int Modulo24(int hours)
    {
        return (hours % 24 + 24) % 24;
    }
}
