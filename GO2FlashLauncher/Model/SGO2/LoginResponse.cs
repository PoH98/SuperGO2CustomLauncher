using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GO2FlashLauncher.Model.SGO2
{

    public class LoginResponse
    {
        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("data")]
        public LoginData Data { get; set; }
    }

    public class LoginData
    {
        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("vip")]
        public int Vip { get; set; }

        [JsonProperty("rank")]
        public string Rank { get; set; }

        [JsonProperty("token")]
        public string Token { get; set; }

        [JsonProperty("maxPlanet")]
        public int MaxPlanet { get; set; }
    }
}
