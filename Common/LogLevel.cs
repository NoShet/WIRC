using System.IO;
using System.Text;

namespace WIRC
{
    public enum LogLevel
    {
        None,
        Notice,
        Error
    }

    public class Logger
    {
        public Logger(TextWriter dest)
        {
            this.LogDest = dest;
        }

        public TextWriter LogDest { get; set; }

        public void Log(LogLevel level, string message)
            => LogDest.WriteLine($"{GetLogLevelString(level),-12}{message}");

        // TODO colorize log levels
        private static string GetLogLevelString(LogLevel level)
        {
            var levelText = level switch
            {
                LogLevel.Notice => "Notice",
                LogLevel.Error => "Error",
                _ => "",
            };

            return $"[{levelText}]";
        }
    }
}
