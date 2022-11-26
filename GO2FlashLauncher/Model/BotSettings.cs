using System.Collections.Generic;

namespace GO2FlashLauncher.Model
{
    internal class BotSettings
    {
        public List<PlanetSettings> PlanetSettings { get; set; } = new List<PlanetSettings>();
        public string CredentialHash { get; set; }
        public string AuthKey { get; set; }
        public int Delays { get; set; } = 800;
        public string DiscordBotToken { get; set; }
        public ulong DiscordUserID { get; set; }
        public string DiscordSecret { get; set; }
    }

    internal class Fleet
    {
        public string Name { get; set; }
        public int Order { get; set; }
        public int RestrictOrder { get; set; }
        public int TrialOrder { get; set; }
        public int ConstellationOrder { get; set; }
    }

    internal class PlanetSettings
    {
        public int Instance { get; set; } = 1;
        public bool RunBot { get; set; } = true;
        public bool RunWheel { get; set; } = true;
        public List<Fleet> Fleets { get; set; } = new List<Fleet>();
        public decimal HaltOn { get; set; } = 300000;
        public decimal InstanceHitCount { get; set; } = 1;
        public bool RestrictFight { get; set; } = false;
        public bool TrialFight { get; set; } = false;
        public int RestrictLevel { get; set; } = 1;
        public int TrialMaxLv { get; set; } = 10;
        public bool SpinWheel { get; set; } = false;
        public int MinVouchers { get; set; } = 5;
        public bool SpinWithVouchers { get; set; } = true;
        public bool ConstellationFight { get; set; } = false;
        public int ConstellationLevel { get; set; } = 1;
        public int ConstellationStage { get; set; } = 1;
        public long ConstellationCount { get; set; } = 3;
        public int PlanetId { get; set; } = -1;
        public string PlanetName { get; set; }
    }
}
