using System.Buffers.Binary;
using DNSGame.Core.Services;

namespace DNSGame.Core.Tests;

public sealed class DnsUdpProbeTests
{
    [Fact]
    public void BuildQuery_CreatesARecordQuestion()
    {
        var query = DnsUdpProbe.BuildQuery(0x1234, "example.com");

        Assert.Equal(0x1234, BinaryPrimitives.ReadUInt16BigEndian(query.AsSpan(0, 2)));
        Assert.Equal(1, BinaryPrimitives.ReadUInt16BigEndian(query.AsSpan(4, 2)));
        Assert.Equal(1, BinaryPrimitives.ReadUInt16BigEndian(query.AsSpan(query.Length - 4, 2)));
        Assert.Equal(1, BinaryPrimitives.ReadUInt16BigEndian(query.AsSpan(query.Length - 2, 2)));
    }

    [Theory]
    [InlineData(0x8180, true)]
    [InlineData(0x0100, false)]
    [InlineData(0x8183, false)]
    public void IsSuccessfulResponse_ValidatesFlags(int flags, bool expected)
    {
        var response = new byte[12];
        BinaryPrimitives.WriteUInt16BigEndian(response.AsSpan(0, 2), 42);
        BinaryPrimitives.WriteUInt16BigEndian(response.AsSpan(2, 2), (ushort)flags);

        Assert.Equal(expected, DnsUdpProbe.IsSuccessfulResponse(response, 42));
    }

    [Fact]
    public void IsSuccessfulResponse_RejectsWrongTransactionId()
    {
        var response = new byte[12];
        BinaryPrimitives.WriteUInt16BigEndian(response.AsSpan(0, 2), 11);
        BinaryPrimitives.WriteUInt16BigEndian(response.AsSpan(2, 2), 0x8180);

        Assert.False(DnsUdpProbe.IsSuccessfulResponse(response, 12));
    }
}
