using System.Buffers.Binary;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using DNSGame.Core.Models;

namespace DNSGame.Core.Services;

public interface IDnsProbe
{
    Task<DnsProbeResult> ProbeAsync(IPAddress address, int timeoutMilliseconds, CancellationToken cancellationToken);
}

public sealed class DnsUdpProbe : IDnsProbe
{
    public async Task<DnsProbeResult> ProbeAsync(IPAddress address, int timeoutMilliseconds, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(address);
        var transactionId = (ushort)Random.Shared.Next(1, ushort.MaxValue);
        var request = BuildQuery(transactionId, "cloudflare.com");

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(timeoutMilliseconds);
        using var udp = new UdpClient(AddressFamily.InterNetwork);

        try
        {
            udp.Connect(address, 53);
            var stopwatch = Stopwatch.StartNew();
            await udp.SendAsync(request, timeout.Token).ConfigureAwait(false);
            var response = await udp.ReceiveAsync(timeout.Token).ConfigureAwait(false);
            stopwatch.Stop();

            var valid = IsSuccessfulResponse(response.Buffer, transactionId);
            return new DnsProbeResult(valid, valid ? stopwatch.Elapsed.TotalMilliseconds : null, valid ? null : "Invalid DNS response.");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new DnsProbeResult(false, null, "DNS request timed out.");
        }
        catch (SocketException exception)
        {
            return new DnsProbeResult(false, null, exception.Message);
        }
    }

    public static byte[] BuildQuery(ushort transactionId, string hostName)
    {
        var labels = hostName.Split('.', StringSplitOptions.RemoveEmptyEntries);
        var length = 12 + labels.Sum(label => 1 + label.Length) + 1 + 4;
        var buffer = new byte[length];
        BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(0, 2), transactionId);
        BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(2, 2), 0x0100);
        BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(4, 2), 1);

        var offset = 12;
        foreach (var label in labels)
        {
            if (label.Length > 63)
            {
                throw new ArgumentException("A DNS label cannot exceed 63 characters.", nameof(hostName));
            }

            buffer[offset++] = (byte)label.Length;
            foreach (var character in label)
            {
                buffer[offset++] = checked((byte)character);
            }
        }

        buffer[offset++] = 0;
        BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(offset, 2), 1);
        BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(offset + 2, 2), 1);
        return buffer;
    }

    public static bool IsSuccessfulResponse(ReadOnlySpan<byte> response, ushort transactionId)
    {
        if (response.Length < 12 || BinaryPrimitives.ReadUInt16BigEndian(response[..2]) != transactionId)
        {
            return false;
        }

        var flags = BinaryPrimitives.ReadUInt16BigEndian(response.Slice(2, 2));
        var isResponse = (flags & 0x8000) != 0;
        var responseCode = flags & 0x000F;
        return isResponse && responseCode == 0;
    }
}
