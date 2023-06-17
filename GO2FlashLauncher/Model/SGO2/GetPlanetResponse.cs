using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace GO2FlashLauncher.Model.SGO2
{
    public class Datum
    {
        [JsonProperty("userId")]
        public int UserId { get; set; }

        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("star")]
        public string Star { get; set; }

        [JsonProperty("ground")]
        public int Ground { get; set; }

        [JsonProperty("resources")]
        public Resources Resources { get; set; }
    }

    public class Resources
    {
        [JsonProperty("gold")]
        public int? Gold { get; set; }

        [JsonProperty("he3")]
        public int? He3 { get; set; }

        [JsonProperty("metal")]
        public int? Metal { get; set; }

        [JsonProperty("vouchers")]
        public int? Vouchers { get; set; }

        [JsonProperty("mallPoints")]
        public int? MallPoints { get; set; }

        [JsonProperty("coupons")]
        public int? Coupons { get; set; }

        [JsonProperty("corsairs")]
        public int? Corsairs { get; set; }

        [JsonProperty("honor")]
        public int? Honor { get; set; }

        [JsonProperty("badge")]
        public int? Badge { get; set; }

        [JsonProperty("championPoints")]
        public int? ChampionPoints { get; set; }

        [JsonProperty("freeSpins")]
        public int? FreeSpins { get; set; }

        [JsonProperty("lastSpin")]
        public DateTime? LastSpin { get; set; }
    }

    public class GetPlanetResponse
    {
        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("data")]
        public List<Datum> Data { get; set; }
    }
}
