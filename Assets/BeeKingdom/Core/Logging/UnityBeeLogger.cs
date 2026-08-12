using UnityEngine;

namespace BeeKingdom.Core.Logging
{
    /// <summary>
    /// Central Unity logger for infrastructure code. Gameplay systems should receive IBeeLogger by injection.
    /// </summary>
    public sealed class UnityBeeLogger : IBeeLogger
    {
        public BeeLogLevel MinimumLevel { get; set; }

        public UnityBeeLogger(BeeLogLevel minimumLevel)
        {
            MinimumLevel = minimumLevel;
        }

        public void Log(BeeLogLevel level, string message)
        {
            if (level < MinimumLevel)
            {
                return;
            }

            string formattedMessage = $"[BeeKingdom] {message}";
            switch (level)
            {
                case BeeLogLevel.Debug:
                case BeeLogLevel.Info:
                    Debug.Log(formattedMessage);
                    break;
                case BeeLogLevel.Warning:
                    Debug.LogWarning(formattedMessage);
                    break;
                case BeeLogLevel.Error:
                    Debug.LogError(formattedMessage);
                    break;
            }
        }
    }
}
