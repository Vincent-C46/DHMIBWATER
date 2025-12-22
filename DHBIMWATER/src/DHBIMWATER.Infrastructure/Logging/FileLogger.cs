using System.Text;

namespace DHBIMWATER.Infrastructure.Logging
{
    public class FileLogger : ILogger
    {
        private readonly string _logFilePath;
        private readonly object _lock = new object();

        public FileLogger(string logFilePath)
        {
            _logFilePath = logFilePath;
        }

        public void Error(string message, Exception ex = null)
        {
            var fullMessage = ex == null ? message : $"{message}\nException: {ex}";
        }

        public void Info(string message)
        {
            Write("INFO", message);
        }

        public void Warn(string message)
        {
            Write("WARN", message);
        }

        private void Write(string level, string message)
        {
            try
            {
                StringBuilder log = new StringBuilder();
                log.AppendLine("--------------------------------------------------");
                log.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}]");
                log.AppendLine(message);

                // lock은 멀티스레드 환경에서 동시 접근을 방지
                lock (_lock)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(_logFilePath));
                    File.AppendAllText(_logFilePath, log.ToString());
                }
            }
            catch
            {
                // 로깅 실패 시 무시
            }
        }
    }
}
