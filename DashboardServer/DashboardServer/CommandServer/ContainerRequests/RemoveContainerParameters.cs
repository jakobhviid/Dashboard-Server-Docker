using Newtonsoft.Json;

namespace DashboardServer.CommandServer.ContainerRequests
{
    public class RemoveContainerParameters
    {
        [JsonProperty(Required = Required.Always)]
        public ContainerActionType Action { get; set; }

        [JsonProperty(Required = Required.Always)]
        public string ContainerId { get; set; }
        [JsonProperty(Required = Required.Default)]
        public bool RemoveVolumes { get; set; }
    }
}
