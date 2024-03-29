﻿using GO2FlashLauncher.Model.SGO2;
using GO2FlashLauncher.Models;
using Newtonsoft.Json;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GO2FlashLauncher.Service
{
    internal class GO2HttpService
    {
        private readonly HttpClient httpClient;
        private readonly string Host;
        public GO2HttpService()
        {
            httpClient = new HttpClient();
            if (!File.Exists("host"))
            {
                File.WriteAllText("host", "https://api.guerradenaves.lat");
            }
            Host = File.ReadAllText("host");
            _ = httpClient.DefaultRequestHeaders.TryAddWithoutValidation("origin", Host);
            _ = httpClient.DefaultRequestHeaders.TryAddWithoutValidation("referer", Host);
            _ = httpClient.DefaultRequestHeaders.TryAddWithoutValidation("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) supergo2-beta/1.0.0-beta Chrome/85.0.4183.121 Electron/10.1.3 Safari/537.36");
        }

        public void SetToken(string token)
        {
            _ = httpClient.DefaultRequestHeaders.TryAddWithoutValidation("authorization", token);
        }
        public async Task<LoginResponse> Login(string username, string password)
        {
            HttpResponseMessage response = await httpClient.PostAsync(Host + "/login/login/account", new StringContent(JsonConvert.SerializeObject(new
            {
                username,
                password
            }), Encoding.UTF8, "application/json"));
            LoginResponse result = JsonConvert.DeserializeObject<LoginResponse>(await response.Content.ReadAsStringAsync());
            if (result.Code != 200)
            {
                throw new HttpRequestException(result.Message);
            }
            if (httpClient.DefaultRequestHeaders.Contains("authorization"))
            {
                _ = httpClient.DefaultRequestHeaders.Remove("authorization");
            }
            _ = httpClient.DefaultRequestHeaders.TryAddWithoutValidation("authorization", result.Data.Token);
            return result;
        }

        public async Task CreatePlanet(string username, int ground)
        {
            HttpResponseMessage response = await httpClient.PostAsync(Host + "/accounts/create/user", new StringContent(JsonConvert.SerializeObject(new
            {
                ground,
                username
            }), Encoding.UTF8, "application/json"));
            LoginResponse result = JsonConvert.DeserializeObject<LoginResponse>(await response.Content.ReadAsStringAsync());
            if (result.Code != 200)
            {
                throw new HttpRequestException(result.Message);
            }
        }

        public async Task<GetFrameResponse> GetIFrameUrl(int id)
        {
            HttpResponseMessage response = await httpClient.GetAsync(Host + "/accounts/play/user/" + id);
            GetFrameResponse result = JsonConvert.DeserializeObject<GetFrameResponse>(await response.Content.ReadAsStringAsync());
            return result;
        }

        public async Task<GetPlanetResponse> GetPlanets()
        {
            HttpResponseMessage response = await httpClient.GetAsync(Host + "/accounts/list/user");
            GetPlanetResponse result = JsonConvert.DeserializeObject<GetPlanetResponse>(await response.Content.ReadAsStringAsync());
            return result;
        }

        public async Task<OnlinePlayers> GetOnlinePlayers()
        {
            HttpResponseMessage response = await httpClient.GetAsync(Host + "/metrics/online");
            OnlinePlayers online = JsonConvert.DeserializeObject<OnlinePlayers>(await response.Content.ReadAsStringAsync());
            return online;
        }
    }
}
