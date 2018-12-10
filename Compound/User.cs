using Newtonsoft.Json;

namespace Compound
{
    public class User
    {
        [JsonProperty("firstName")]
        public string FirstName;

        [JsonProperty("lastName")]
        public string LastName;

        [JsonProperty("emailAddress")]
        public string EmailAddress;

        [JsonProperty("ipAddress")]
        public string IpAddress;

        [JsonProperty("userAgent")]
        public string UserAgent;

        [JsonProperty("custom")]
        public string Custom;

    }
}