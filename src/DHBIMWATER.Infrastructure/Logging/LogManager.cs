using System.IO;

namespace DHBIMWATER.Infrastructure.Logging
{
    public static class LogManager
    {
        private static ILogger _logger;

        public static ILogger Logger
        {
            get
            {
                if(_logger == null)
                {
                    string logPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "DHBIMWATER",
                        "Logs",
                        "DHBIMWATER.log");

                    _logger = new FileLogger(logPath);
                }

                return _logger;
            }

        }
    }
}
