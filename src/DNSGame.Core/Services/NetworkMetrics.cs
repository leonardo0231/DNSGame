using DNSGame.Core.Models;

namespace DNSGame.Core.Services;

public static class NetworkMetrics
{
    public static PingStatistics Calculate(IReadOnlyList<double?> roundTrips)
    {
        ArgumentNullException.ThrowIfNull(roundTrips);
        if (roundTrips.Count == 0)
        {
            throw new ArgumentException("At least one sample is required.", nameof(roundTrips));
        }

        var successful = roundTrips.Where(value => value.HasValue).Select(value => value!.Value).ToArray();
        var loss = (roundTrips.Count - successful.Length) * 100d / roundTrips.Count;
        if (successful.Length == 0)
        {
            return new PingStatistics(roundTrips.Count, 0, loss, null, null, null, null);
        }

        double jitter = 0;
        if (successful.Length > 1)
        {
            jitter = successful.Zip(successful.Skip(1), (left, right) => Math.Abs(right - left)).Average();
        }

        return new PingStatistics(
            roundTrips.Count,
            successful.Length,
            loss,
            successful.Min(),
            successful.Average(),
            successful.Max(),
            jitter);
    }

    public static double? CalculateScore(PingStatistics ping, DnsProbeResult dns)
    {
        if (ping.AverageMilliseconds is null)
        {
            return null;
        }

        var jitter = ping.JitterMilliseconds ?? 0;
        var dnsPenalty = dns.IsDnsServer ? (dns.ResponseMilliseconds ?? 0) * 0.15 : 35;
        return Math.Round(ping.AverageMilliseconds.Value + (jitter * 2) + (ping.PacketLossPercent * 4) + dnsPenalty, 2);
    }
}
