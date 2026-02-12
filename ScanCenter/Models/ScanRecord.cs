using System;

namespace ScanCenter.Models
{
    [Serializable]
    public class ScanRecord
    {
        public string FilePath { get; set; }
        public string DisplayName { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
