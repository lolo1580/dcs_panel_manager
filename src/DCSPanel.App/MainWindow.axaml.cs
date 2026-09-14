using Avalonia.Controls;
using DCSPanel.App.ViewModels;
using DCSPanel.Hardware.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace DCSPanel.App;

public partial class MainWindow : Window
{
    private readonly IHardwareService? _hardwareService;

    public MainWindow()
    {
        InitializeComponent();
    }

    [ActivatorUtilitiesConstructor]
    public MainWindow(MainWindowViewModel viewModel, IHardwareService hardwareService) : this()
    {
        DataContext = viewModel;
        _hardwareService = hardwareService;
    }

    protected override async void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        if (_hardwareService is not null)
        {
            await _hardwareService.StartAsync();
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        (DataContext as IDisposable)?.Dispose();
        _hardwareService?.StopAsync().GetAwaiter().GetResult();
        base.OnClosed(e);
    }
}
