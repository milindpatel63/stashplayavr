using Newtonsoft.Json;

namespace PlayaApiV2.Model
{
    public class AuthenticationCode
    {
        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("token")]
        public string Token { get; set; }

        [JsonProperty("expires_at")]
        public Timestamp ExpiresAt { get; set; }
    }
}
