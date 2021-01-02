using Newtonsoft.Json;

namespace DashboardServer.CommandServer.ContainerResponses
{
    public class LogContainerResponse
    {
        [JsonProperty(Required = Required.Always)]
        public string ServerName { get; set; }
        [JsonProperty(Required = Required.Always)]
        public string ContainerId { get; set; }
        [JsonProperty(Required = Required.Always)]
        public string RawData { get; set; }
    }
}