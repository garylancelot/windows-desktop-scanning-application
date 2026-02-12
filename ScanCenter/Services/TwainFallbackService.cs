using System;
using System.Diagnostics;
using System.Windows;

namespace ScanCenter.Services
{
    public class TwainFallbackService
    {
        public bool TryPromptFallback(string reason)
        {
            var result = MessageBox.Show(
                "WIA scan failed. Would you like to open the Windows acquisition wizard (TWAIN/WIA fallback path)?\n\n" +
                "Reason: " + reason,
                "Fallback Acquisition",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                Process.Start("wiaacmgr.exe", "/AcquireImage");
                return true;
            }

            return false;
        }
    }
}
