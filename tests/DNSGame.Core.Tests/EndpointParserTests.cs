using DNSGame.Core.Services;

namespace DNSGame.Core.Tests;

public sealed class EndpointParserTests
{
    [Fact]
    public void Parse_AcceptsIpv4AndDnsNames_AndRemovesDuplicates()
    {
        var result = EndpointParser.Parse([" 1.1.1.1 ", "Example.com", "example.COM", "# comment", "bad value"]);

        Assert.Collection(
            result,
            endpoint =>
            {
                Assert.Equal("1.1.1.1", endpoint.Value);
                Assert.True(endpoint.IsIpAddress);
            },
            endpoint =>
            {
                Assert.Equal("Example.com", endpoint.Value);
                Assert.False(endpoint.IsIpAddress);
            });
    }

    [Fact]
    public void Parse_TrimsTrailingDnsDot()
    {
        var endpoint = Assert.Single(EndpointParser.Parse(["dns.example.com."]));
        Assert.Equal("dns.example.com", endpoint.Value);
    }
}
