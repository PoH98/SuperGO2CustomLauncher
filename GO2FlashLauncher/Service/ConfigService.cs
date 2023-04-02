using GO2FlashLauncher.Model;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;

namespace GO2FlashLauncher.Service
{
    internal class ConfigService
    {
        private static ConfigService _instance;
        public static ConfigService Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ConfigService();
                }
                return _instance;
            }
        }

        private BotSettings _config;
        public BotSettings Config
        {
            get
            {
                if (_config == null)
                {
                    _config = JsonConvert.DeserializeObject<BotSettings>(File.ReadAllText(ConfigPath));
                    _config.PlanetSettings = _config.PlanetSettings.Distinct().ToList();
                }
                return _config;
            }
        }

        public string ConfigFolder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GalaxyOrbit4", "Profile");
        public string ConfigPath => Path.Combine(ConfigFolder, "config.json");
        public ConfigService()
        {
            if (!Directory.Exists(ConfigFolder))
            {
                _ = Directory.CreateDirectory(ConfigFolder);
            }
            if (!File.Exists(ConfigPath))
            {
                File.WriteAllText(ConfigPath, JsonConvert.SerializeObject(new BotSettings()));
            }
        }

        public void Save()
        {
            if (!Directory.Exists(ConfigFolder))
            {
                _ = Directory.CreateDirectory(ConfigFolder);
            }
            File.WriteAllText(ConfigPath, JsonConvert.SerializeObject(_config));
        }
        public void Reset()
        {
            if (!Directory.Exists(ConfigFolder))
            {
                _ = Directory.CreateDirectory(ConfigFolder);
            }
            File.WriteAllText(ConfigPath, JsonConvert.SerializeObject(new BotSettings()));
            _config = new BotSettings();
        }
        public void Load()
        {
            if (!Directory.Exists(ConfigFolder))
            {
                _ = Directory.CreateDirectory(ConfigFolder);
            }
            if (!File.Exists(ConfigPath))
            {
                File.WriteAllText(ConfigPath, JsonConvert.SerializeObject(new BotSettings()));
            }
            _config = JsonConvert.DeserializeObject<BotSettings>(File.ReadAllText(ConfigPath));
        }
    }
}
