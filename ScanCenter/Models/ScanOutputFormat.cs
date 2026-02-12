namespace ScanCenter.Models
{
    public enum ScanOutputFormat
    {
        Pdf,
        Jpeg,
        Png,
        Tiff
    }

    public class ScanOptions
    {
        public int Dpi { get; set; }
        public string ColorMode { get; set; }
        public bool ColorCorrection { get; set; }
    }
}
