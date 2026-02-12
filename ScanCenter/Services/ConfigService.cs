using ScanCenter.Models;
using System;
using System.IO;
using System.Runtime.Serialization.Json;

namespace ScanCenter.Services
{
    public class ConfigService
    {
        private readonly string _settingsPath;

        public ConfigService()
        {
            var baseFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ScanCenterOutput");
            Directory.CreateDirectory(baseFolder);
            _settingsPath = Path.Combine(baseFolder, "settings.json");
        }

        public AppConfig Load()
        {
            if (!File.Exists(_settingsPath))
            {
                var cfg = new AppConfig();
                Save(cfg);
                return cfg;
            }

            using (var stream = File.OpenRead(_settingsPath))
            {
                var serializer = new DataContractJsonSerializer(typeof(AppConfig));
                return (AppConfig)serializer.ReadObject(stream);
            }
        }

        public void Save(AppConfig config)
        {
            using (var stream = File.Create(_settingsPath))
            {
                var serializer = new DataContractJsonSerializer(typeof(AppConfig));
                serializer.WriteObject(stream, config);
            }
        }
    }
}
