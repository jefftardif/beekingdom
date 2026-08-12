namespace BeeKingdom.Core.Logging
{
    public interface IBeeLogger
    {
        BeeLogLevel MinimumLevel { get; set; }
        void Log(BeeLogLevel level, string message);
    }
}
