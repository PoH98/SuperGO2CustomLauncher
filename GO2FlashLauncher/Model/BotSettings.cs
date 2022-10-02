using System.Collections.Generic;

namespace GO2FlashLauncher.Model
{
    internal class BotSettings
    {
        public int Instance { get; set; } = 1;
        public bool RunBot { get; set; } = true;
        public bool RunWheel { get; set; } = true;
        public List<Fleet> Fleets { get; set; } = new List<Fleet>();
        public string CredentialHash { get; set; }
        public decimal HaltOn { get; set; }
        public decimal InstanceHitCount { get; set; } = 1;
        public string AuthKey { get; set; }
        public int PlanetId { get; set; } = -1;
        public int Delays { get; set; } = 800;
        public bool RestrictFight { get; set; } = false;
        public bool TrialFight { get; set; } = false;
        public int RestrictLevel { get; set; } = 1;
        public int TrialMaxLv { get; set; } = 10;
    }

    internal class Fleet
    {
        public string Name { get; set; }
        public int Order { get; set; }
    }
}
