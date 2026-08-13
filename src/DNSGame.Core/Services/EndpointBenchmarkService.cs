using DNSGame.Core.Models;

namespace DNSGame.Core.Services;

public sealed class EndpointBenchmarkService(IPingProbe pingProbe, IDnsProbe dnsProbe)
{
    public async Task<IReadOnlyList<EndpointBenchmark>> BenchmarkAsync(
        IEnumerable<EndpointCandidate> endpoints,
        ProbeOptions options,
        IProgress<EndpointBenchmark>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        options.Validate();
        using var limiter = new SemaphoreSlim(options.MaxParallelism);

        var tasks = endpoints.Select(async endpoint =>
        {
            await limiter.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var ping = await pingProbe.ProbeAsync(endpoint.Value, options, cancellationToken).ConfigureAwait(false);
                var dns = ping.Address is null
                    ? new DnsProbeResult(false, null, "Endpoint did not resolve.")
                    : await dnsProbe.ProbeAsync(ping.Address, options.TimeoutMilliseconds, cancellationToken).ConfigureAwait(false);

                var score = NetworkMetrics.CalculateScore(ping.Statistics, dns);
                var status = ping.Statistics.Received == 0 ? "Unreachable" : dns.IsDnsServer ? "DNS ready" : "Reachable only";
                var result = new EndpointBenchmark(endpoint, ping.Address, ping.Statistics, dns, score, status);
                progress?.Report(result);
                return result;
            }
            finally
            {
                limiter.Release();
            }
        });

        var results = await Task.WhenAll(tasks).ConfigureAwait(false);
        return results.OrderBy(result => result.Score ?? double.MaxValue).ThenBy(result => result.Endpoint.Value).ToArray();
    }
}
