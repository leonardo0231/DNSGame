using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using DNSGame.Core.Models;

namespace DNSGame.Core.Services;

public interface IPingProbe
{
    Task<PingProbeResult> ProbeAsync(string target, ProbeOptions options, CancellationToken cancellationToken);
}

public sealed class PingProbe : IPingProbe
{
    private static readonly byte[] Payload = "DNSGame-Probe"u8.ToArray();

    public async Task<PingProbeResult> ProbeAsync(string target, ProbeOptions options, CancellationToken cancellationToken)
    {
        options.Validate();

        try
        {
            var address = await ResolveIpv4Async(target, cancellationToken).ConfigureAwait(false);
            if (address is null)
            {
                return Failed(options.Samples, "No IPv4 address was resolved.");
            }

            var samples = new List<double?>(options.Samples);
            using var ping = new Ping();
            var pingOptions = new PingOptions(64, true);

            for (var index = 0; index < options.Samples; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    var reply = await ping.SendPingAsync(address, options.TimeoutMilliseconds, Payload, pingOptions)
                        .WaitAsync(cancellationToken)
                        .ConfigureAwait(false);
                    samples.Add(reply.Status == IPStatus.Success ? reply.RoundtripTime : null);
                }
                catch (PingException)
                {
                    samples.Add(null);
                }
            }

            return new PingProbeResult(address, NetworkMetrics.Calculate(samples));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (exception is SocketException or ArgumentException)
        {
            return Failed(options.Samples, exception.Message);
        }
    }

    private static async Task<IPAddress?> ResolveIpv4Async(string target, CancellationToken cancellationToken)
    {
        if (IPAddress.TryParse(target, out var parsed))
        {
            return parsed.AddressFamily == AddressFamily.InterNetwork ? parsed : null;
        }

        var addresses = await Dns.GetHostAddressesAsync(target, cancellationToken).ConfigureAwait(false);
        return addresses.FirstOrDefault(address => address.AddressFamily == AddressFamily.InterNetwork);
    }

    private static PingProbeResult Failed(int samples, string error) =>
        new(null, new PingStatistics(samples, 0, 100, null, null, null, null), error);
}
