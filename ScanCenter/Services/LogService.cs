using System;
using System.IO;

namespace ScanCenter.Services
{
    public class LogService
    {
        public string LogPath { get; }

        public LogService()
        {
            var logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ScanCenterOutput", "logs");
            Directory.CreateDirectory(logDir);
            LogPath = Path.Combine(logDir, $"ScanCenter_{DateTime.Now:yyyyMMdd}.log");
        }

        public void Info(string message) => Write("INFO", message);
        public void Error(string message, Exception ex) => Write("ERROR", $"{message} :: {ex.Message} :: {ex}");

        private void Write(string level, string message)
        {
            File.AppendAllText(LogPath, $"{DateTime.Now:O} [{level}] {message}{Environment.NewLine}");
        }
    }
}
