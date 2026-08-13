using DNSGame.Core.Models;
using DNSGame.Core.Services;

namespace DNSGame.Core.Tests;

public sealed class NetworkMetricsTests
{
    [Fact]
    public void Calculate_ComputesLatencyJitterAndLoss()
    {
        var result = NetworkMetrics.Calculate([10, 20, null, 15]);

        Assert.Equal(4, result.Sent);
        Assert.Equal(3, result.Received);
        Assert.Equal(25, result.PacketLossPercent);
        Assert.Equal(10, result.MinimumMilliseconds);
        Assert.Equal(15, result.AverageMilliseconds);
        Assert.Equal(20, result.MaximumMilliseconds);
        Assert.Equal(7.5, result.JitterMilliseconds);
    }

    [Fact]
    public void Calculate_AllLost_ReturnsNoLatency()
    {
        var result = NetworkMetrics.Calculate([null, null]);

        Assert.Equal(100, result.PacketLossPercent);
        Assert.Null(result.AverageMilliseconds);
        Assert.Null(result.JitterMilliseconds);
    }

    [Fact]
    public void CalculateScore_PenalizesLossAndNonDnsEndpoints()
    {
        var ping = new PingStatistics(4, 4, 0, 10, 20, 30, 2);
        var dns = new DnsProbeResult(true, 10);
        var nonDns = new DnsProbeResult(false, null);

        Assert.Equal(25.5, NetworkMetrics.CalculateScore(ping, dns));
        Assert.True(NetworkMetrics.CalculateScore(ping, nonDns) > NetworkMetrics.CalculateScore(ping, dns));
    }
}
