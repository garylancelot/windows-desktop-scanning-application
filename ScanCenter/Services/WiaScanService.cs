using ScanCenter.Models;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using WIA;

namespace ScanCenter.Services
{
    public class WiaScanService
    {
        private const string WIA_FORMAT_JPEG = "{B96B3CAE-0728-11D3-9D7B-0000F81EF32E}";

        public Task<string> ScanToFileAsync(ScanOptions options, string outputPath, bool previewOnly)
        {
            return Task.Run(() =>
            {
                var manager = new DeviceManager();
                DeviceInfo scannerInfo = null;
                foreach (DeviceInfo info in manager.DeviceInfos)
                {
                    if (info.Type == WiaDeviceType.ScannerDeviceType)
                    {
                        scannerInfo = info;
                        break;
                    }
                }

                if (scannerInfo == null)
                {
                    throw new InvalidOperationException("No WIA scanner found.");
                }

                Device scanner = scannerInfo.Connect();
                Item item = scanner.Items[1];

                SetItemProperty(item.Properties, 6147, options.Dpi);
                SetItemProperty(item.Properties, 6148, options.Dpi);
                SetItemProperty(item.Properties, 6146, GetIntent(options.ColorMode));

                var dialog = new CommonDialog();
                ImageFile image = dialog.ShowTransfer(item, WIA_FORMAT_JPEG, false) as ImageFile;
                if (image == null)
                {
                    throw new COMException("Failed to transfer image from scanner.");
                }

                image.SaveFile(outputPath);
                return outputPath;
            });
        }

        private int GetIntent(string mode)
        {
            switch (mode)
            {
                case "Black and White": return 4;
                case "Grayscale": return 2;
                default: return 1;
            }
        }

        private void SetItemProperty(IProperties properties, object propName, object propValue)
        {
            Property prop = properties.get_Item(ref propName);
            prop.set_Value(ref propValue);
        }
    }
}
