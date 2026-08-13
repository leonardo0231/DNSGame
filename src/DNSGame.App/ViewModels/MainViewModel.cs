using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using DNSGame.App.Infrastructure;
using DNSGame.Core.Models;
using DNSGame.Core.Services;

namespace DNSGame.App.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly EndpointBenchmarkService benchmarkService;
    private readonly WindowsDnsManager dnsManager;
    private readonly IReadOnlyList<EndpointCandidate> endpoints;
    private CancellationTokenSource? cancellation;
    private AdapterInfo? selectedAdapter;
    private EndpointRowViewModel? selectedResult;
    private string statusMessage = "برای پیدا کردن بهترین endpoint، تست کامل را اجرا کنید.";
    private bool isBusy;
    private double progressPercent;

    public MainViewModel(EndpointBenchmarkService benchmarkService, WindowsDnsManager dnsManager)
    {
        this.benchmarkService = benchmarkService;
        this.dnsManager = dnsManager;

        var endpointPath = Path.Combine(AppContext.BaseDirectory, "Assets", "endpoints.txt");
        endpoints = EndpointParser.Parse(File.Exists(endpointPath) ? File.ReadLines(endpointPath) : []);
        Adapters = new ObservableCollection<AdapterInfo>(LoadAdapters());
        SelectedAdapter = Adapters.FirstOrDefault();
        Results = new ObservableCollection<EndpointRowViewModel>();

        StartBenchmarkCommand = new AsyncCommand(StartBenchmarkAsync, () => !IsBusy && endpoints.Count > 0);
        CancelCommand = new RelayCommand(() => cancellation?.Cancel(), () => IsBusy);
        ApplyDnsCommand = new AsyncCommand(ApplyDnsAsync, () => !IsBusy && SelectedAdapter is not null && SelectedResult?.DnsReady == true);
        RestoreDnsCommand = new AsyncCommand(RestoreDnsAsync, () => !IsBusy && SelectedAdapter is not null);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<AdapterInfo> Adapters { get; }

    public ObservableCollection<EndpointRowViewModel> Results { get; }

    public AsyncCommand StartBenchmarkCommand { get; }

    public RelayCommand CancelCommand { get; }

    public AsyncCommand ApplyDnsCommand { get; }

    public AsyncCommand RestoreDnsCommand { get; }

    public AdapterInfo? SelectedAdapter
    {
        get => selectedAdapter;
        set
        {
            if (SetField(ref selectedAdapter, value))
            {
                RaiseCommands();
            }
        }
    }

    public EndpointRowViewModel? SelectedResult
    {
        get => selectedResult;
        set
        {
            if (SetField(ref selectedResult, value))
            {
                RaiseCommands();
            }
        }
    }

    public string StatusMessage
    {
        get => statusMessage;
        private set => SetField(ref statusMessage, value);
    }

    public bool IsBusy
    {
        get => isBusy;
        private set
        {
            if (SetField(ref isBusy, value))
            {
                RaiseCommands();
            }
        }
    }

    public double ProgressPercent
    {
        get => progressPercent;
        private set => SetField(ref progressPercent, value);
    }

    public void Dispose()
    {
        cancellation?.Cancel();
        cancellation?.Dispose();
        cancellation = null;
    }

    private async Task StartBenchmarkAsync()
    {
        IsBusy = true;
        ProgressPercent = 0;
        Results.Clear();
        cancellation = new CancellationTokenSource();
        var completed = 0;
        var progress = new Progress<EndpointBenchmark>(result =>
        {
            Results.Add(new EndpointRowViewModel(result));
            completed++;
            ProgressPercent = completed * 100d / endpoints.Count;
            StatusMessage = $"در حال تست: {completed} از {endpoints.Count}";
        });

        try
        {
            var results = await benchmarkService.BenchmarkAsync(endpoints, new ProbeOptions(), progress, cancellation.Token);
            var orderedRows = results.Select(result => new EndpointRowViewModel(result)).ToArray();
            Results.Clear();
            foreach (var row in orderedRows)
            {
                Results.Add(row);
            }

            SelectedResult = Results.FirstOrDefault(row => row.DnsReady) ?? Results.FirstOrDefault();
            var dnsCount = Results.Count(row => row.DnsReady);
            StatusMessage = $"تست تمام شد: {dnsCount} DNS معتبر از {Results.Count} endpoint پیدا شد.";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "تست توسط کاربر لغو شد.";
        }
        catch (Exception exception)
        {
            StatusMessage = $"خطا در تست شبکه: {exception.Message}";
        }
        finally
        {
            cancellation.Dispose();
            cancellation = null;
            IsBusy = false;
        }
    }

    private async Task ApplyDnsAsync()
    {
        if (SelectedAdapter is null || SelectedResult?.AddressValue is null || !SelectedResult.DnsReady)
        {
            StatusMessage = "فقط endpointهایی که تست DNS را پاس کرده‌اند قابل اعمال هستند.";
            return;
        }

        IsBusy = true;
        try
        {
            var servers = new List<IPAddress> { SelectedResult.AddressValue };
            var secondary = Results.FirstOrDefault(row => row.DnsReady && row.AddressValue is not null && !row.AddressValue.Equals(SelectedResult.AddressValue));
            if (secondary?.AddressValue is not null)
            {
                servers.Add(secondary.AddressValue);
            }

            await dnsManager.ApplyAsync(SelectedAdapter, servers, CancellationToken.None);
            StatusMessage = $"DNS روی {SelectedAdapter.DisplayName} اعمال شد: {string.Join("، ", servers)}";
        }
        catch (Exception exception)
        {
            StatusMessage = $"اعمال DNS ناموفق بود: {exception.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task RestoreDnsAsync()
    {
        if (SelectedAdapter is null)
        {
            return;
        }

        IsBusy = true;
        try
        {
            await dnsManager.RestoreAsync(SelectedAdapter, CancellationToken.None);
            StatusMessage = $"تنظیمات DNS قبلی برای {SelectedAdapter.DisplayName} بازگردانی شد.";
        }
        catch (Exception exception)
        {
            StatusMessage = $"بازگردانی DNS ناموفق بود: {exception.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static IEnumerable<AdapterInfo> LoadAdapters() => NetworkInterface.GetAllNetworkInterfaces()
        .Where(adapter => adapter.OperationalStatus == OperationalStatus.Up && adapter.NetworkInterfaceType is not NetworkInterfaceType.Loopback)
        .Select(adapter => new
        {
            Adapter = adapter,
            Properties = adapter.GetIPProperties().GetIPv4Properties(),
        })
        .Where(item => item.Properties is not null)
        .Select(item => new AdapterInfo(item.Properties!.Index, item.Adapter.Name, item.Adapter.Description))
        .OrderBy(adapter => adapter.DisplayName);

    private void RaiseCommands()
    {
        StartBenchmarkCommand.RaiseCanExecuteChanged();
        CancelCommand.RaiseCanExecuteChanged();
        ApplyDnsCommand.RaiseCanExecuteChanged();
        RestoreDnsCommand.RaiseCanExecuteChanged();
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}

public sealed record AdapterInfo(int InterfaceIndex, string Name, string Description)
{
    public string DisplayName => $"{Name} — {Description}";
}

public sealed class EndpointRowViewModel(EndpointBenchmark benchmark)
{
    public string Endpoint { get; } = benchmark.Endpoint.Value;

    public string Address { get; } = benchmark.ResolvedAddress?.ToString() ?? "—";

    public IPAddress? AddressValue { get; } = benchmark.ResolvedAddress;

    public string AveragePing { get; } = Format(benchmark.Ping.AverageMilliseconds, "ms");

    public string Jitter { get; } = Format(benchmark.Ping.JitterMilliseconds, "ms");

    public string PacketLoss { get; } = $"{benchmark.Ping.PacketLossPercent:0.#}%";

    public string DnsResponse { get; } = benchmark.Dns.IsDnsServer ? Format(benchmark.Dns.ResponseMilliseconds, "ms") : "No";

    public string Score { get; } = benchmark.Score?.ToString("0.##", CultureInfo.InvariantCulture) ?? "—";

    public string Status { get; } = benchmark.Status;

    public bool DnsReady { get; } = benchmark.Dns.IsDnsServer;

    private static string Format(double? value, string suffix) => value is null ? "—" : $"{value:0.##} {suffix}";
}
