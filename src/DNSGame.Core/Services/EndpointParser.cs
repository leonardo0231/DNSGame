using System.Net;
using DNSGame.Core.Models;

namespace DNSGame.Core.Services;

public static class EndpointParser
{
    public static IReadOnlyList<EndpointCandidate> Parse(IEnumerable<string> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var endpoints = new List<EndpointCandidate>();

        foreach (var rawLine in lines)
        {
            var value = rawLine.Trim().TrimEnd('.');
            if (string.IsNullOrWhiteSpace(value) || value.StartsWith('#'))
            {
                continue;
            }

            var isIp = IPAddress.TryParse(value, out var address) && address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork;
            var isHost = Uri.CheckHostName(value) == UriHostNameType.Dns;
            if ((!isIp && !isHost) || !seen.Add(value))
            {
                continue;
            }

            endpoints.Add(new EndpointCandidate(value, isIp));
        }

        return endpoints;
    }
}
