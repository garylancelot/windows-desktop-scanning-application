using PdfSharp.Drawing;
using PdfSharp.Pdf;
using System.Collections.Generic;

namespace ScanCenter.Services
{
    public class PdfService
    {
        public void SaveMultiPagePdf(IEnumerable<string> imagePaths, string outputPath)
        {
            var document = new PdfDocument();
            foreach (var imagePath in imagePaths)
            {
                var page = document.AddPage();
                using (var image = XImage.FromFile(imagePath))
                using (var gfx = XGraphics.FromPdfPage(page))
                {
                    page.Width = image.PixelWidth * 72 / image.HorizontalResolution;
                    page.Height = image.PixelHeight * 72 / image.VerticalResolution;
                    gfx.DrawImage(image, 0, 0, page.Width, page.Height);
                }
            }
            document.Save(outputPath);
        }
    }
}
