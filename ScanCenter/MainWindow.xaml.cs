using Microsoft.Win32;
using ScanCenter.Models;
using ScanCenter.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ScanCenter
{
    public partial class MainWindow : Window
    {
        private readonly ConfigService _configService = new ConfigService();
        private readonly LogService _logService = new LogService();
        private readonly DeviceService _deviceService = new DeviceService();
        private readonly WiaScanService _wiaScanService = new WiaScanService();
        private readonly TwainFallbackService _twainFallbackService = new TwainFallbackService();
        private readonly PdfService _pdfService = new PdfService();

        private AppConfig _config;
        private BitmapSource _currentPreview;
        private double _zoom = 1.0;
        private Point? _cropStart;
        private Rect? _cropRect;
        private readonly List<string> _pdfPageQueue = new List<string>();
        private ScanRecord _selectedRecent;

        public MainWindow()
        {
            InitializeComponent();
            _config = _configService.Load();
            TemplateTextBox.Text = _config.NamingTemplate;
            RefreshDevices();
            LoadRecents();
            SetStatus("Ready.");
        }

        private void NavigateHome_Click(object sender, RoutedEventArgs e) => SectionTitleText.Text = "Home";
        private void NavigateScan_Click(object sender, RoutedEventArgs e) => SectionTitleText.Text = "Scan";
        private void NavigateOutput_Click(object sender, RoutedEventArgs e) => SectionTitleText.Text = "Save / Share";
        private void NavigateSettings_Click(object sender, RoutedEventArgs e)
        {
            SectionTitleText.Text = "Settings";
            var dialog = new SaveFileDialog { Filter = "Folder Placeholder|*.folder", FileName = "Select this and click Save" };
            MessageBox.Show("Use the Output Folder button from the workflow to choose your folder.", "Settings", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        private void NavigateHelp_Click(object sender, RoutedEventArgs e)
        {
            SectionTitleText.Text = "Help";
            OpenLocalHelp();
        }
        private void NavigateDeviceStatus_Click(object sender, RoutedEventArgs e) => SectionTitleText.Text = "Device Status";

        private void StartDocumentWorkflow_Click(object sender, RoutedEventArgs e)
        {
            SectionTitleText.Text = "Document Workflow";
            SelectCombo(DpiCombo, "200");
            SelectCombo(ColorModeCombo, "Grayscale");
            SelectCombo(OutputFormatCombo, "PDF");
        }

        private void StartPhotoWorkflow_Click(object sender, RoutedEventArgs e)
        {
            SectionTitleText.Text = "Photo Workflow";
            SelectCombo(DpiCombo, "600");
            SelectCombo(ColorModeCombo, "Color");
            SelectCombo(OutputFormatCombo, "JPEG");
            ColorCorrectionCheck.IsChecked = true;
        }

        private void StartPdfWorkflow_Click(object sender, RoutedEventArgs e)
        {
            SectionTitleText.Text = "Scan to PDF Workflow";
            SelectCombo(OutputFormatCombo, "PDF");
            _pdfPageQueue.Clear();
            SetStatus("PDF mode ready. Use Start Scan then Scan Next Page as needed.");
        }

        private void SelectCombo(ComboBox comboBox, string value)
        {
            foreach (ComboBoxItem item in comboBox.Items)
            {
                if ((item.Content?.ToString() ?? string.Empty).Equals(value, StringComparison.OrdinalIgnoreCase))
                {
                    comboBox.SelectedItem = item;
                    return;
                }
            }
        }

        private void RefreshDevices_Click(object sender, RoutedEventArgs e) => RefreshDevices();

        private void RefreshDevices()
        {
            try
            {
                var status = _deviceService.GetDeviceStatus();
                DeviceNameText.Text = $"Device: {status.DeviceName}";
                ConnectionStateText.Text = $"Connection: {(status.IsConnected ? "Connected" : "Not connected")}";
                DriverStateText.Text = $"Driver status: {status.DriverStatus}";
                _logService.Info("Device refresh complete.");
            }
            catch (Exception ex)
            {
                _logService.Error("Device refresh failed", ex);
                SetStatus("Could not refresh devices. Use Fix It for steps.");
            }
        }

        private void TestConnection_Click(object sender, RoutedEventArgs e)
        {
            var result = _deviceService.TestConnection();
            ConnectionStateText.Text = $"Connection: {(result ? "Connected" : "Failed")}";
            SetStatus(result ? "Scanner connection successful." : "Scanner test failed. Check driver and USB cable.");
        }

        private async void Preview_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var tempPath = Path.Combine(Path.GetTempPath(), $"scan_preview_{DateTime.Now:yyyyMMddHHmmss}.jpg");
                var options = BuildOptions();
                var path = await _wiaScanService.ScanToFileAsync(options, tempPath, previewOnly: true);
                LoadPreview(path);
                SetStatus("Preview captured.");
            }
            catch (Exception ex)
            {
                _logService.Error("Preview failed", ex);
                if (_twainFallbackService.TryPromptFallback(ex.Message))
                {
                    SetStatus("WIA preview failed; fallback tool launched.");
                }
                else
                {
                    SetStatus("Preview failed. Ensure WIA service and driver are installed.");
                }
            }
        }

        private async void StartScan_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var options = BuildOptions();
                var outputFormat = GetSelectedText(OutputFormatCombo).ToUpperInvariant();
                var baseName = ResolveNameTemplate();
                Directory.CreateDirectory(_config.OutputFolder);

                var tempImage = Path.Combine(Path.GetTempPath(), $"scan_{Guid.NewGuid():N}.jpg");
                var scannedImagePath = await _wiaScanService.ScanToFileAsync(options, tempImage, previewOnly: false);
                var preparedImage = ApplyCrop(scannedImagePath);

                string finalPath;
                if (outputFormat == "PDF")
                {
                    _pdfPageQueue.Add(preparedImage);
                    finalPath = Path.Combine(_config.OutputFolder, baseName + ".pdf");
                    _pdfService.SaveMultiPagePdf(_pdfPageQueue, finalPath);
                    SetStatus($"Scanned page added to PDF ({_pdfPageQueue.Count} pages).");
                }
                else
                {
                    var ext = outputFormat == "JPEG" ? "jpg" : outputFormat.ToLowerInvariant();
                    finalPath = Path.Combine(_config.OutputFolder, baseName + "." + ext);
                    File.Copy(preparedImage, finalPath, true);
                    SetStatus("Scan saved.");
                }

                LastScanText.Text = $"Last scan: {DateTime.Now:G}";
                _logService.Info($"Saved scan: {finalPath}");
                AddRecent(finalPath);
            }
            catch (Exception ex)
            {
                _logService.Error("Scan failed", ex);
                if (_twainFallbackService.TryPromptFallback(ex.Message))
                {
                    SetStatus("WIA scan failed; fallback acquisition launched.");
                }
                else
                {
                    SetStatus("Scan failed. Click Fix It for guided diagnostics.");
                }
            }
        }

        private async void ScanNextPage_Click(object sender, RoutedEventArgs e)
        {
            SelectCombo(OutputFormatCombo, "PDF");
            await System.Threading.Tasks.Task.Run(() => Dispatcher.Invoke(() => StartScan_Click(sender, e)));
        }

        private string ApplyCrop(string imagePath)
        {
            if (!_cropRect.HasValue || _currentPreview == null)
            {
                return imagePath;
            }

            var bitmap = new BitmapImage(new Uri(imagePath));
            var xScale = bitmap.PixelWidth / PreviewImage.ActualWidth;
            var yScale = bitmap.PixelHeight / PreviewImage.ActualHeight;

            var rect = _cropRect.Value;
            int x = (int)Math.Max(0, rect.X * xScale);
            int y = (int)Math.Max(0, rect.Y * yScale);
            int width = (int)Math.Min(bitmap.PixelWidth - x, rect.Width * xScale);
            int height = (int)Math.Min(bitmap.PixelHeight - y, rect.Height * yScale);

            if (width < 5 || height < 5) return imagePath;

            var cropped = new CroppedBitmap(bitmap, new Int32Rect(x, y, width, height));
            var output = Path.Combine(Path.GetTempPath(), $"cropped_{Guid.NewGuid():N}.png");
            using (var fs = new FileStream(output, FileMode.Create))
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(cropped));
                encoder.Save(fs);
            }
            return output;
        }

        private ScanOptions BuildOptions()
        {
            int dpi = int.Parse(GetSelectedText(DpiCombo));
            string mode = GetSelectedText(ColorModeCombo);
            return new ScanOptions
            {
                Dpi = dpi,
                ColorMode = mode,
                ColorCorrection = ColorCorrectionCheck.IsChecked == true
            };
        }

        private string ResolveNameTemplate()
        {
            var template = TemplateTextBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(template))
            {
                template = "Scan_{yyyyMMdd_HHmmss}";
            }
            template = template.Replace("{yyyyMMdd_HHmmss}", DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            _config.NamingTemplate = template;
            _configService.Save(_config);
            return template;
        }

        private string GetSelectedText(ComboBox combo) => ((ComboBoxItem)combo.SelectedItem).Content.ToString();

        private void OpenOutputFolder_Click(object sender, RoutedEventArgs e)
        {
            Directory.CreateDirectory(_config.OutputFolder);
            Process.Start("explorer.exe", _config.OutputFolder);
        }

        private void CopyDiagnostics_Click(object sender, RoutedEventArgs e)
        {
            var diagnostics = _deviceService.BuildDiagnosticsReport(_logService.LogPath);
            Clipboard.SetText(diagnostics);
            SetStatus("Diagnostics copied to clipboard.");
        }

        private void ZoomOut_Click(object sender, RoutedEventArgs e) => SetZoom(_zoom - 0.1);
        private void ZoomIn_Click(object sender, RoutedEventArgs e) => SetZoom(_zoom + 0.1);

        private void SetZoom(double value)
        {
            _zoom = Math.Max(0.2, Math.Min(4.0, value));
            PreviewImage.LayoutTransform = new ScaleTransform(_zoom, _zoom);
            ZoomText.Text = $"{_zoom:P0}";
        }

        private void LoadPreview(string imagePath)
        {
            _currentPreview = new BitmapImage(new Uri(imagePath));
            PreviewImage.Source = _currentPreview;
            PreviewCanvas.Width = _currentPreview.PixelWidth;
            PreviewCanvas.Height = _currentPreview.PixelHeight;
            _cropRect = null;
            CropRectangle.Visibility = Visibility.Collapsed;
        }

        private void PreviewCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _cropStart = e.GetPosition(PreviewCanvas);
            CropRectangle.Visibility = Visibility.Visible;
            Canvas.SetLeft(CropRectangle, _cropStart.Value.X);
            Canvas.SetTop(CropRectangle, _cropStart.Value.Y);
            CropRectangle.Width = 0;
            CropRectangle.Height = 0;
        }

        private void PreviewCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_cropStart.HasValue || e.LeftButton != MouseButtonState.Pressed) return;
            var current = e.GetPosition(PreviewCanvas);
            var x = Math.Min(current.X, _cropStart.Value.X);
            var y = Math.Min(current.Y, _cropStart.Value.Y);
            var width = Math.Abs(current.X - _cropStart.Value.X);
            var height = Math.Abs(current.Y - _cropStart.Value.Y);
            Canvas.SetLeft(CropRectangle, x);
            Canvas.SetTop(CropRectangle, y);
            CropRectangle.Width = width;
            CropRectangle.Height = height;
        }

        private void PreviewCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_cropStart.HasValue) return;
            var end = e.GetPosition(PreviewCanvas);
            _cropRect = new Rect(_cropStart.Value, end);
            _cropRect = new Rect(
                Math.Min(_cropRect.Value.X, end.X),
                Math.Min(_cropRect.Value.Y, end.Y),
                Math.Abs(end.X - _cropStart.Value.X),
                Math.Abs(end.Y - _cropStart.Value.Y));
            _cropStart = null;
        }

        private void FixIt_Click(object sender, RoutedEventArgs e) => OpenLocalHelp();

        private void OpenLocalHelp()
        {
            var helpPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Help", "FixIt.html");
            if (File.Exists(helpPath))
            {
                Process.Start(helpPath);
            }
            else
            {
                MessageBox.Show("Open TROUBLESHOOTING.md from the installer pack for offline diagnostics.", "Fix It", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void LoadRecents()
        {
            RecentScansList.ItemsSource = _config.RecentScans;
        }

        private void AddRecent(string path)
        {
            _config.RecentScans.Insert(0, new ScanRecord { FilePath = path, DisplayName = System.IO.Path.GetFileName(path), Timestamp = DateTime.Now });
            _config.RecentScans = _config.RecentScans.Take(50).ToList();
            _configService.Save(_config);
            RecentScansList.ItemsSource = null;
            RecentScansList.ItemsSource = _config.RecentScans;
        }

        private void RecentScansList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedRecent = RecentScansList.SelectedItem as ScanRecord;
        }

        private void OpenRecent_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedRecent != null && File.Exists(_selectedRecent.FilePath))
            {
                Process.Start(_selectedRecent.FilePath);
            }
        }

        private void RevealRecent_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedRecent != null && File.Exists(_selectedRecent.FilePath))
            {
                Process.Start("explorer.exe", $"/select,\"{_selectedRecent.FilePath}\"");
            }
        }

        private void SetStatus(string message)
        {
            StatusMessageText.Text = message;
        }
    }
}
