using DCSPanel.Core.State;

namespace DCSPanel.DCSBIOS.Abstractions;

public sealed record DcsBiosStateChanged(
    ConnectionStatus Status,
    string? Aircraft = null);

public sealed record DcsBiosValueChanged(
    string ControlId,
    object? Value,
    DateTimeOffset Timestamp);

public interface IDcsBiosClient : IAsyncDisposable
{
    event EventHandler<DcsBiosStateChanged>? StateChanged;
    event EventHandler<DcsBiosValueChanged>? ValueChanged;

    ConnectionStatus Status { get; }

    Task ConnectAsync(CancellationToken cancellationToken = default);
    Task DisconnectAsync(CancellationToken cancellationToken = default);
    ValueTask SendCommandAsync(string controlId, string argument, CancellationToken cancellationToken = default);
}

public interface IDcsBiosMetadataProvider
{
    Task<IReadOnlyList<DcsBiosControlMetadata>> GetControlsAsync(
        string aircraft,
        CancellationToken cancellationToken = default);
}

public sealed record DcsBiosControlMetadata(
    string Identifier,
    string Description,
    string Category,
    IReadOnlyList<string> Inputs,
    string OutputType);
