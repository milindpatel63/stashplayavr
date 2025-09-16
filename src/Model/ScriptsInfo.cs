using Newtonsoft.Json;

namespace PlayaApiV2.Model
{
    public class ScriptsInfo
    {
        [JsonProperty("token")]
        public string Token { get; set; }
    }
}
