using Newtonsoft.Json;

namespace DashboardServer.CommandServer.ContainerResponses
{
    public class ContainerResponse
    {
        [JsonProperty(Required = Required.Always)]
        public int ResponseStatusCode { get; set; }

        [JsonProperty(Required = Required.Always)]
        public string Message { get; set; }
    }
}
