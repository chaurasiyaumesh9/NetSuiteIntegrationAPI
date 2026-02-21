using System.Text.Json.Serialization;

namespace NetSuiteIntegrationAPI.Models
{
    public class NetSuiteOptions
    {
        public string AccountId { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string PrivateKey { get; set; } = string.Empty;
        public string CertificateId { get; set; } = string.Empty;
    }

    public class NsToken
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("expires_in")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int ExpiresIn { get; set; }
    }
}
