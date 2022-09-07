using System.Collections.Generic;

namespace GO2FlashLauncher.Model
{
    internal class BotSettings
    {
        public int Instance { get; set; } = 1;
        public bool RunBot { get; set; } = true;
        public List<Fleet> Fleets { get; set; } = new List<Fleet>();
        public string CredentialHash { get; set; }
    }

    internal class Fleet
    {
        public string Name { get; set; }
        public int Order { get; set; }
    }
}
