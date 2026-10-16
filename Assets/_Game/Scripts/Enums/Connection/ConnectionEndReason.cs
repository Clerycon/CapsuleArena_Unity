public enum ConnectionEndReason
{
    None,
    StartFailed, // Started Server/Host/Client But Failed (bool)
    CouldNotConnect, // Connection Loss While Connecting
    ConnectionLost, // Connection Loss While Connected
    ServerStopped, // Server Stopped
    NetworkError, // Network Subsystem Has Crashed (Transport)
    SessionEnded, // You Left On Yourself
}