using DCSPanel.Core.Events;
using DCSPanel.Hardware.Logitech;
using DCSPanel.Hardware.Models;
using Microsoft.Extensions.Logging;

namespace DCSPanel.Hardware.Probe;

internal static partial class Program
{
    public static async Task<int> Main(string[] args)
    {
        var durationSeconds = args.Length > 0 && int.TryParse(args[0], out var parsed) ? parsed : 20;
        using var loggerFactory = LoggerFactory.Create(builder => builder
            .SetMinimumLevel(LogLevel.Debug)
            .AddSimpleConsole(options =>
            {
                options.SingleLine = true;
                options.TimestampFormat = "HH:mm:ss.fff ";
            }));

        var logger = loggerFactory.CreateLogger<LogitechHidService>();
        var probeLogger = loggerFactory.CreateLogger("HardwareProbe");
        var activityHub = new ActivityHub();
        await using var service = new LogitechHidService(logger, activityHub);
        var connected = 0;
        var rawReports = 0;
        var decodedInputs = 0;
        var errors = 0;

        service.HardwareEventOccurred += (_, hardwareEvent) =>
        {
            switch (hardwareEvent.Kind)
            {
                case HardwareEventKind.Connected:
                    Interlocked.Increment(ref connected);
                    break;
                case HardwareEventKind.RawInput:
                    Interlocked.Increment(ref rawReports);
                    break;
                case HardwareEventKind.Input:
                    Interlocked.Increment(ref decodedInputs);
                    break;
                case HardwareEventKind.Error:
                    Interlocked.Increment(ref errors);
                    break;
            }
        };

        LogProbeStarted(probeLogger, durationSeconds);
        await service.StartAsync().ConfigureAwait(false);
        await Task.Delay(TimeSpan.FromSeconds(durationSeconds)).ConfigureAwait(false);
        await service.StopAsync().ConfigureAwait(false);
        LogProbeSummary(probeLogger, connected, rawReports, decodedInputs, errors);

        return connected >= 2 && rawReports > 0 && errors == 0 ? 0 : 1;
    }

    [LoggerMessage(EventId = 2001, Level = LogLevel.Information,
        Message = "Test HID démarré pour {DurationSeconds} secondes. Actionnez les commandes des deux panneaux.")]
    private static partial void LogProbeStarted(ILogger logger, int durationSeconds);

    [LoggerMessage(EventId = 2002, Level = LogLevel.Information,
        Message = "Résumé : connexions={Connected}, rapports bruts={RawReports}, entrées décodées={DecodedInputs}, erreurs={Errors}")]
    private static partial void LogProbeSummary(ILogger logger, int connected, int rawReports, int decodedInputs, int errors);
}
