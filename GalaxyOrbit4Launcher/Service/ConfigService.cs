using GalaxyOrbit4Launcher.Models;
using Newtonsoft.Json;
using System;
using System.IO;

namespace GalaxyOrbit4Launcher.Service
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

        private Config _config;
        public Config Config
        {
            get
            {
                if (_config == null)
                {
                    _config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(ConfigPath));
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
                File.WriteAllText(ConfigPath, JsonConvert.SerializeObject(new Config()));
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
            File.WriteAllText(ConfigPath, JsonConvert.SerializeObject(new Config()));
            _config = new Config();
        }
        public void Load()
        {
            if (!Directory.Exists(ConfigFolder))
            {
                _ = Directory.CreateDirectory(ConfigFolder);
            }
            if (!File.Exists(ConfigPath))
            {
                File.WriteAllText(ConfigPath, JsonConvert.SerializeObject(new Config()));
            }
            _config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(ConfigPath));
        }
    }
}
