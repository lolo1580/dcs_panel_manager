namespace DCSPanel.Core.State;

public enum ConnectionStatus
{
    Disconnected,
    Connecting,
    Connected,
    Faulted
}

public sealed record ApplicationState(
    ConnectionStatus DcsWorld,
    ConnectionStatus DcsBios,
    string? Aircraft,
    string ActiveProfile)
{
    public static ApplicationState Initial { get; } = new(
        ConnectionStatus.Disconnected,
        ConnectionStatus.Disconnected,
        null,
        "Generic");
}
