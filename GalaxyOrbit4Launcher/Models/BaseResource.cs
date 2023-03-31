using Newtonsoft.Json;

namespace GalaxyOrbit4Launcher.Models
{
    internal class BaseResource
    {
        [JsonProperty("guid")]
        public int Guid { get; set; }
        [JsonProperty("metal")]
        public long Metal { get; set; }
        [JsonProperty("he3")]
        public long He3 { get; set; }
        [JsonProperty("gold")]
        public long Gold { get; set; }
        [JsonProperty("mallPoints")]
        public int MallPoints { get; set; }
        [JsonProperty("vouchers")]
        public int Vouchers { get; set; }

        public override string ToString()
        {
            return $"Metal: {Metal}\nHE3: {He3}\nGold: {Gold}\nMP: {MallPoints}\nVouchers: {Vouchers}";
        }
    }
}
