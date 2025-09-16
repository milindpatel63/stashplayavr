using Newtonsoft.Json;

namespace PlayaApiV2.Model
{
    public class SignInCodeRequest
    {
        [JsonProperty("code")]
        public string Code { get; set; }
        
        [JsonProperty("token")]
        public string Token { get; set; }
    }
}
