using System.Reflection;
using Avalonia.Threading;
using DCSPanel.App.ViewModels;
using DCSPanel.Core.Events;
using DCSPanel.Core.Mapping;
using DCSPanel.Core.State;
using DCSPanel.DCSBIOS;
using DCSPanel.DCSBIOS.Abstractions;
using DCSPanel.Hardware.Abstractions;
using DCSPanel.Hardware.Models;
using DCSPanel.Profiles;

namespace DCSPanel.Core.Tests.Mapping;

public sealed class EditorRegressionTests
{
    [Theory]
    [InlineData(PhysicalInputKind.EncoderClockwise, "+3200")]
    [InlineData(PhysicalInputKind.EncoderCounterClockwise, "-3200")]
    public void EncoderSuggestionsUseExplicitRelativeSigns(PhysicalInputKind kind, string expected)
    {
        var control = CreateControl();
        var argument = control.SuggestArgument(new MappingTriggerOption("Turn", kind, true));
        Assert.Equal(expected, argument);
        Assert.True(control.IsValidArgument(argument));
    }

    [Fact]
    public void SavingOriginalProfilePreservesNewSelectionAndAircraftChangeClearsOldCommands()
    {
        var repository = new JsonProfileRepository(new ProfileValidator());
        using var vm = new MainWindowViewModel(new IdleHardware(), new ActivityHub(),
            new DisconnectedDcsBiosClient(), new PendingMetadata(), new DcsBiosOptions(),
            new ProfileCatalog(repository), repository);
        var original = new ProfileViewModel(StarterProfileFactory.Create("F-16C_50"));
        var other = new ProfileViewModel(StarterProfileFactory.Create("UH-1H"));
        vm.Profiles.Add(original);
        vm.Profiles.Add(other);
        vm.SelectedProfile = other;
        var edited = ProfileMappingEditor.Upsert(original.Profile,
            new InputMapping("LogitechPz55", "MASTER_BAT", PhysicalInputKind.Switch,
                [new ActionDefinition("DCS-BIOS", "MAIN_PWR_SW", "1")], true));

        Invoke(vm, "ApplyUpdatedProfile", original, edited);

        Assert.Same(other, vm.SelectedProfile);
        Assert.Same(other, vm.Profiles[1]);
        Assert.Same(edited, vm.Profiles[0].Profile);

        var oldControl = CreateControl();
        vm.Controls.Add(oldControl);
        vm.WritableControls.Add(oldControl);
        vm.SelectedCommandControl = oldControl;
        Invoke(vm, "OnDcsBiosStateChanged", null, new DcsBiosStateChanged(ConnectionStatus.Connected, "UH-1H"));
        Dispatcher.UIThread.RunJobs();

        Assert.Empty(vm.Controls);
        Assert.Empty(vm.WritableControls);
        Assert.Null(vm.SelectedCommandControl);
        Assert.False(vm.AddMappingCommand.CanExecute(null));
    }

    private static void Invoke(MainWindowViewModel vm, string method, params object?[] arguments) =>
        typeof(MainWindowViewModel).GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(vm, arguments);

    private static DcsBiosControlViewModel CreateControl() => new(new DcsBiosControlMetadata(
        "DIAL", "Dial", "Panel", "limited_dial",
        [new DcsBiosInputMetadata("set_state", 65535, null),
         new DcsBiosInputMetadata("variable_step", 65535, null, 3200)], []));

    private sealed class PendingMetadata : IDcsBiosMetadataProvider
    {
        public string? MetadataDirectory => null;
        public Task<IReadOnlyList<string>> GetAircraftAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<string>>([]);
        public async Task<IReadOnlyList<DcsBiosControlMetadata>> GetControlsAsync(string aircraft,
            CancellationToken cancellationToken = default)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return [];
        }
    }

    private sealed class IdleHardware : IHardwareService
    {
        public event EventHandler<HardwareEvent>? HardwareEventOccurred { add { } remove { } }
        public IReadOnlyList<DeviceDescriptor> Devices => [];
        public Task StartAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SetOutputAsync(DeviceId deviceId, Output output, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
