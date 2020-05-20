using Newtonsoft.Json;

namespace DashboardServer.CommandServer.ContainerRequests
{
    public class StopContainerParameters
    {
        [JsonProperty(Required = Required.Always)]
        public ContainerActionType Action { get; set; }

        [JsonProperty(Required = Required.Always)]
        public string ContainerId { get; set; }
    }
}
