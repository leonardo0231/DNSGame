using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using DNSGame.App.ViewModels;

namespace DNSGame.App.Infrastructure;

public sealed class WindowsDnsManager
{
    private static readonly string StateDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DNSGame");
    private static readonly string SnapshotPath = Path.Combine(StateDirectory, "dns-snapshot.json");

    public async Task ApplyAsync(AdapterInfo adapter, IReadOnlyList<IPAddress> servers, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(adapter);
        ArgumentNullException.ThrowIfNull(servers);
        if (servers.Count == 0 || servers.Any(address => address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork))
        {
            throw new ArgumentException("At least one IPv4 DNS server is required.", nameof(servers));
        }

        Directory.CreateDirectory(StateDirectory);
        if (!File.Exists(SnapshotPath))
        {
            var currentJson = await RunPowerShellAsync(
                $"Get-DnsClientServerAddress -InterfaceIndex {adapter.InterfaceIndex} -AddressFamily IPv4 | Select-Object InterfaceIndex,ServerAddresses | ConvertTo-Json -Compress",
                cancellationToken);
            var current = JsonSerializer.Deserialize<PowerShellDnsState>(currentJson) ?? new PowerShellDnsState(adapter.InterfaceIndex, []);
            var snapshot = new DnsSnapshot(adapter.InterfaceIndex, adapter.Name, current.ServerAddresses ?? []);
            await File.WriteAllTextAsync(SnapshotPath, JsonSerializer.Serialize(snapshot), cancellationToken);
        }

        var addresses = string.Join(',', servers.Select(address => $"'{address}'"));
        await RunPowerShellAsync(
            $"$ErrorActionPreference='Stop'; Set-DnsClientServerAddress -InterfaceIndex {adapter.InterfaceIndex} -ServerAddresses @({addresses}); Clear-DnsClientCache",
            cancellationToken);
    }

    public async Task RestoreAsync(AdapterInfo adapter, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(adapter);
        if (!File.Exists(SnapshotPath))
        {
            await RunPowerShellAsync(
                $"$ErrorActionPreference='Stop'; Set-DnsClientServerAddress -InterfaceIndex {adapter.InterfaceIndex} -ResetServerAddresses; Clear-DnsClientCache",
                cancellationToken);
            return;
        }

        var json = await File.ReadAllTextAsync(SnapshotPath, cancellationToken);
        var snapshot = JsonSerializer.Deserialize<DnsSnapshot>(json) ?? throw new InvalidDataException("The DNS snapshot is invalid.");
        if (snapshot.InterfaceIndex != adapter.InterfaceIndex)
        {
            throw new InvalidOperationException("The saved snapshot belongs to another network adapter.");
        }

        var command = snapshot.ServerAddresses.Length == 0
            ? $"Set-DnsClientServerAddress -InterfaceIndex {adapter.InterfaceIndex} -ResetServerAddresses"
            : $"Set-DnsClientServerAddress -InterfaceIndex {adapter.InterfaceIndex} -ServerAddresses @({string.Join(',', snapshot.ServerAddresses.Select(address => $"'{IPAddress.Parse(address)}'"))})";

        await RunPowerShellAsync($"$ErrorActionPreference='Stop'; {command}; Clear-DnsClientCache", cancellationToken);
        File.Delete(SnapshotPath);
    }

    private static async Task<string> RunPowerShellAsync(string script, CancellationToken cancellationToken)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("DNS configuration is supported only on Windows.");
        }

        var encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));
        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        startInfo.ArgumentList.Add("-NoLogo");
        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-NonInteractive");
        startInfo.ArgumentList.Add("-EncodedCommand");
        startInfo.ArgumentList.Add(encoded);

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("PowerShell could not be started.");
        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        var output = await outputTask;
        var error = await errorTask;
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(error) ? "Windows rejected the DNS change." : error.Trim());
        }

        return output.Trim();
    }

    private sealed record DnsSnapshot(int InterfaceIndex, string AdapterName, string[] ServerAddresses);

    private sealed record PowerShellDnsState(int InterfaceIndex, string[]? ServerAddresses);
}
