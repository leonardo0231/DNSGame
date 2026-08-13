using System.Net;

namespace DNSGame.Core.Models;

public sealed record EndpointCandidate(string Value, bool IsIpAddress);

public sealed record ProbeOptions(int Samples = 8, int TimeoutMilliseconds = 1200, int MaxParallelism = 12)
{
    public ProbeOptions Validate()
    {
        if (Samples is < 2 or > 50)
        {
            throw new ArgumentOutOfRangeException(nameof(Samples), "Samples must be between 2 and 50.");
        }

        if (TimeoutMilliseconds is < 250 or > 10_000)
        {
            throw new ArgumentOutOfRangeException(nameof(TimeoutMilliseconds), "Timeout must be between 250 and 10000 ms.");
        }

        if (MaxParallelism is < 1 or > 64)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxParallelism), "Parallelism must be between 1 and 64.");
        }

        return this;
    }
}

public sealed record PingStatistics(
    int Sent,
    int Received,
    double PacketLossPercent,
    double? MinimumMilliseconds,
    double? AverageMilliseconds,
    double? MaximumMilliseconds,
    double? JitterMilliseconds);

public sealed record PingProbeResult(IPAddress? Address, PingStatistics Statistics, string? Error = null);

public sealed record DnsProbeResult(bool IsDnsServer, double? ResponseMilliseconds, string? Error = null);

public sealed record EndpointBenchmark(
    EndpointCandidate Endpoint,
    IPAddress? ResolvedAddress,
    PingStatistics Ping,
    DnsProbeResult Dns,
    double? Score,
    string Status);
