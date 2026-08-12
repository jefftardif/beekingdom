namespace BeeKingdom.Core.Services
{
    public enum ServiceState
    {
        Registered,
        Initializing,
        Initialized,
        Starting,
        Running,
        Paused,
        ShuttingDown,
        Disposed,
        Failed
    }
}
