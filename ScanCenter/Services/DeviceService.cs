using System;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using WIA;

namespace ScanCenter.Services
{
    public class DeviceService
    {
        public DeviceStatus GetDeviceStatus()
        {
            var manager = new DeviceManager();
            string deviceName = "(none found)";
            bool connected = false;
            foreach (DeviceInfo info in manager.DeviceInfos)
            {
                if (info.Type == WiaDeviceType.ScannerDeviceType)
                {
                    deviceName = info.Properties["Name"].get_Value().ToString();
                    connected = true;
                    break;
                }
            }

            var wiaService = new ServiceController("stisvc");
            var driverState = wiaService.Status == ServiceControllerStatus.Running ? "WIA service running" : "WIA service not running";
            return new DeviceStatus { DeviceName = deviceName, IsConnected = connected, DriverStatus = driverState };
        }

        public bool TestConnection()
        {
            return GetDeviceStatus().IsConnected;
        }

        public string BuildDiagnosticsReport(string logPath)
        {
            var status = GetDeviceStatus();
            return $"Scan Center Diagnostics{Environment.NewLine}" +
                   $"Timestamp: {DateTime.Now:O}{Environment.NewLine}" +
                   $"Device: {status.DeviceName}{Environment.NewLine}" +
                   $"Connected: {status.IsConnected}{Environment.NewLine}" +
                   $"Driver/WIA: {status.DriverStatus}{Environment.NewLine}" +
                   $"Log file: {logPath}{Environment.NewLine}" +
                   $"OS: {Environment.OSVersion}{Environment.NewLine}";
        }

        public void EnsureWiaService()
        {
            var service = new ServiceController("stisvc");
            if (service.Status != ServiceControllerStatus.Running)
            {
                Process.Start(new ProcessStartInfo("sc.exe", "config stisvc start= auto") { CreateNoWindow = true, UseShellExecute = false })?.WaitForExit();
                Process.Start(new ProcessStartInfo("net.exe", "start stisvc") { CreateNoWindow = true, UseShellExecute = false })?.WaitForExit();
            }
        }
    }

    public class DeviceStatus
    {
        public string DeviceName { get; set; }
        public bool IsConnected { get; set; }
        public string DriverStatus { get; set; }
    }
}
