using Newtonsoft.Json;

namespace GO2FlashLauncher.Models
{
    public class Data
    {
        [JsonProperty("online")]
        public int Online { get; set; }
    }

    public class OnlinePlayers
    {
        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("data")]
        public Data Data { get; set; }
    }
}
