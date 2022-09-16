using Newtonsoft.Json;

namespace GO2FlashLauncher.Model.SGO2
{
    public class FrameData
    {
        [JsonProperty("sessionKey")]
        public string SessionKey { get; set; }

        [JsonProperty("userId")]
        public int UserId { get; set; }
    }

    public class GetFrameResponse
    {
        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("data")]
        public FrameData Data { get; set; }
    }


}
