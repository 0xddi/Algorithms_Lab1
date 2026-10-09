using System.Linq;

namespace Algorithms.GUI.Models;

/// <summary>
/// Unit used to display measured time. Results are stored in milliseconds
/// and converted only when they are shown on a chart.
/// </summary>
public sealed record TimeUnit(string Symbol, double PerMillisecond)
{
    public static readonly TimeUnit Nanoseconds = new("нс", 1_000_000);
    public static readonly TimeUnit Microseconds = new("мкс", 1_000);
    public static readonly TimeUnit Milliseconds = new("мс", 1);
    public static readonly TimeUnit Seconds = new("с", 0.001);

    /// <summary>
    /// Picks the largest unit in which the maximum value is at least 1,
    /// so chart values read as numbers between 1 and 1000.
    /// </summary>
    public static TimeUnit Pick(double maxMs)
    {
        if (!(maxMs > 0)) return Milliseconds; // empty data, zeros or NaN

        return maxMs switch
        {
            >= 1000 => Seconds,
            >= 1 => Milliseconds,
            >= 0.001 => Microseconds,
            _ => Nanoseconds
        };
    }

    public double FromMs(double ms) => ms * PerMillisecond;

    public double[] FromMs(double[] ms) => ms.Select(FromMs).ToArray();
}
