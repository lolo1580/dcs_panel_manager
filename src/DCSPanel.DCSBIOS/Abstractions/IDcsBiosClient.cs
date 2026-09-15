using DCSPanel.Core.State;

namespace DCSPanel.DCSBIOS.Abstractions;

public sealed record DcsBiosStateChanged(
    ConnectionStatus Status,
    string? Aircraft = null);

public sealed record DcsBiosValueChanged(
    string ControlId,
    object? Value,
    DateTimeOffset Timestamp);

public sealed record DcsBiosDataReceived(
    int ByteCount,
    long PacketNumber,
    DateTimeOffset Timestamp);

public sealed record DcsBiosOutputSubscription(
    string ControlId,
    DcsBiosOutputMetadata Output);

public interface IDcsBiosClient : IAsyncDisposable
{
    event EventHandler<DcsBiosStateChanged>? StateChanged;
    event EventHandler<DcsBiosValueChanged>? ValueChanged;
    event EventHandler<DcsBiosDataReceived>? DataReceived;

    ConnectionStatus Status { get; }
    string? Aircraft { get; }
    long PacketsReceived { get; }
    DateTimeOffset? LastReceivedAt { get; }

    Task ConnectAsync(CancellationToken cancellationToken = default);
    Task DisconnectAsync(CancellationToken cancellationToken = default);
    void SetOutputSubscriptions(IReadOnlyList<DcsBiosOutputSubscription> subscriptions);
    ValueTask SendCommandAsync(string controlId, string argument, CancellationToken cancellationToken = default);
}

public interface IDcsBiosMetadataProvider
{
    string? MetadataDirectory { get; }

    Task<IReadOnlyList<string>> GetAircraftAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DcsBiosControlMetadata>> GetControlsAsync(
        string aircraft,
        CancellationToken cancellationToken = default);
}

public sealed record DcsBiosControlMetadata(
    string Identifier,
    string Description,
    string Category,
    string ControlType,
    IReadOnlyList<DcsBiosInputMetadata> Inputs,
    IReadOnlyList<DcsBiosOutputMetadata> Outputs);

public sealed record DcsBiosInputMetadata(
    string Interface,
    int? MaxValue,
    string? Description,
    int? SuggestedStep = null);

public sealed record DcsBiosOutputMetadata(
    string Type,
    ushort? Address,
    ushort? Mask,
    int? ShiftBy,
    int? MaxValue,
    int? MaxLength,
    string? Description);
