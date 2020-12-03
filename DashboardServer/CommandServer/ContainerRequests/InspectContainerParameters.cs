using Newtonsoft.Json;

namespace DashboardServer.CommandServer.ContainerRequests
{
    public class InspectContainerParameters
    {
        [JsonProperty(Required = Required.Always)]
        public const ContainerActionType Action = ContainerActionType.INSPECT;
        [JsonProperty(Required = Required.Always)]
        public string ContainerId { get; set; }
    }
}