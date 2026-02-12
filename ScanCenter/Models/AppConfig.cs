using System;
using System.Collections.Generic;

namespace ScanCenter.Models
{
    [Serializable]
    public class AppConfig
    {
        public string OutputFolder { get; set; } = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ScanCenterOutput");
        public string NamingTemplate { get; set; } = "Scan_{yyyyMMdd_HHmmss}";
        public List<ScanRecord> RecentScans { get; set; } = new List<ScanRecord>();
    }
}
