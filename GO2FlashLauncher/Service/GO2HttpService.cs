using GO2FlashLauncher.Model.SGO2;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GO2FlashLauncher.Service
{
    internal class GO2HttpService
    {
        private readonly HttpClient httpClient;
        private readonly string Host = "https://api.guerradenaves.lat";
        public GO2HttpService()
        {
            httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("origin", Host);
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("referer", Host);
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) supergo2-beta/1.0.0-beta Chrome/85.0.4183.121 Electron/10.1.3 Safari/537.36");
        }

        public void SetToken(string token)
        {
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("authorization", token);
        }
        public async Task<LoginResponse> Login(string username, string password)
        {
            var response = await httpClient.PostAsync(Host + "/login/login/account", new StringContent(JsonConvert.SerializeObject(new
            {
                username,
                password
            }), Encoding.UTF8, "application/json"));
            var result = JsonConvert.DeserializeObject<LoginResponse>(await response.Content.ReadAsStringAsync());
            if (httpClient.DefaultRequestHeaders.Contains("authorization"))
            {
                httpClient.DefaultRequestHeaders.Remove("authorization");
            }
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("authorization", result.Data.Token);
            return result;
        }

        public async Task<GetFrameResponse> GetIFrameUrl(int id)
        {
            var response = await httpClient.GetAsync(Host + "/account/play/user/" + id);
            return JsonConvert.DeserializeObject<GetFrameResponse>(await response.Content.ReadAsStringAsync());
        }

        public async Task<GetPlanetResponse> GetPlanets()
        {
            var response = await httpClient.GetAsync(Host + "/account/list/user");
            return JsonConvert.DeserializeObject<GetPlanetResponse>(await response.Content.ReadAsStringAsync());
        }
    }
}
