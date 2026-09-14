using Avalonia.Controls;
using DCSPanel.App.ViewModels;
using DCSPanel.DCSBIOS.Abstractions;
using DCSPanel.Hardware.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace DCSPanel.App;

public partial class MainWindow : Window
{
    private readonly IHardwareService? _hardwareService;
    private readonly IDcsBiosClient? _dcsBiosClient;

    public MainWindow()
    {
        InitializeComponent();
    }

    [ActivatorUtilitiesConstructor]
    public MainWindow(
        MainWindowViewModel viewModel,
        IHardwareService hardwareService,
        IDcsBiosClient dcsBiosClient) : this()
    {
        DataContext = viewModel;
        _hardwareService = hardwareService;
        _dcsBiosClient = dcsBiosClient;
    }

    protected override async void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        if (DataContext is MainWindowViewModel viewModel)
        {
            await viewModel.InitializeAsync();
        }

        if (_hardwareService is not null)
        {
            await _hardwareService.StartAsync();
        }

        if (_dcsBiosClient is not null)
        {
            try
            {
                await _dcsBiosClient.ConnectAsync();
            }
            catch
            {
                // The client publishes its fault to logging and Live Monitor.
                // Hardware monitoring must remain available when DCS-BIOS cannot bind.
            }
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        (DataContext as IDisposable)?.Dispose();
        _dcsBiosClient?.DisconnectAsync().GetAwaiter().GetResult();
        _hardwareService?.StopAsync().GetAwaiter().GetResult();
        base.OnClosed(e);
    }
}
