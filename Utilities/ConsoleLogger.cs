using System;
using SlatedGameToolkit.Framework.Logging;

namespace SkinnerBox.Utilities
{
    public class ConsoleLogger : ILogListener
    {
        public LogLevel Level => LogLevel.DEBUG;

        public void LogMessage(string message, DateTime time, LogLevel level)
        {
            Console.WriteLine(string.Format("[{0}] [{1}]: {2}", time.ToString("H:mm:ss"), level, message));
        }
    }
}