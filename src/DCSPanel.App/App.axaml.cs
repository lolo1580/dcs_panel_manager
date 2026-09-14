using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DCSPanel.App.ViewModels;
using DCSPanel.Core.Events;
using DCSPanel.DCSBIOS;
using DCSPanel.DCSBIOS.Abstractions;
using DCSPanel.Hardware.Abstractions;
using DCSPanel.Hardware.Logitech;
using DCSPanel.Profiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DCSPanel.App;

public partial class App : Application
{
    private ServiceProvider? _services;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddSimpleConsole(options => options.SingleLine = true));
            services.AddSingleton<ActivityHub>();
            services.AddSingleton<IActivitySink>(provider => provider.GetRequiredService<ActivityHub>());
            services.AddSingleton<IHardwareService, LogitechHidService>();
            services.AddSingleton<DcsBiosOptions>();
            services.AddSingleton<IDcsBiosMetadataProvider, JsonDcsBiosMetadataProvider>();
            services.AddSingleton<IDcsBiosClient, UdpDcsBiosClient>();
            services.AddSingleton<ProfileValidator>();
            services.AddSingleton<JsonProfileRepository>();
            services.AddSingleton<MainWindowViewModel>();
            services.AddSingleton<MainWindow>();

            _services = services.BuildServiceProvider();
            desktop.MainWindow = _services.GetRequiredService<MainWindow>();
            desktop.Exit += (_, _) => _services.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
