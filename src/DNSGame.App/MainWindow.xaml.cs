using System.Windows;
using DNSGame.App.Infrastructure;
using DNSGame.App.ViewModels;
using DNSGame.Core.Services;

namespace DNSGame.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel(
            new EndpointBenchmarkService(new PingProbe(), new DnsUdpProbe()),
            new WindowsDnsManager());
    }
}
