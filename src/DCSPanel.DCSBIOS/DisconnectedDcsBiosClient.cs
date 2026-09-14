using DCSPanel.Core.State;
using DCSPanel.DCSBIOS.Abstractions;

namespace DCSPanel.DCSBIOS;

/// <summary>
/// Safe milestone implementation. It models the boundary without opening sockets or sending commands.
/// </summary>
public sealed class DisconnectedDcsBiosClient : IDcsBiosClient
{
    public event EventHandler<DcsBiosStateChanged>? StateChanged
    {
        add { }
        remove { }
    }

    public event EventHandler<DcsBiosValueChanged>? ValueChanged
    {
        add { }
        remove { }
    }

    public ConnectionStatus Status => ConnectionStatus.Disconnected;

    public Task ConnectAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task DisconnectAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public ValueTask SendCommandAsync(string controlId, string argument, CancellationToken cancellationToken = default) =>
        ValueTask.FromException(new NotSupportedException("DCS-BIOS command transmission is disabled in milestone 1."));

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
