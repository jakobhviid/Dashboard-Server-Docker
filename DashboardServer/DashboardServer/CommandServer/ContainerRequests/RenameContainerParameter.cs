using Newtonsoft.Json;

namespace DashboardServer.CommandServer.ContainerRequests
{
    public class RenameContainerParameter
    {
        [JsonProperty(Required = Required.Always)]
        public ContainerActionType Action { get; set; }
        [JsonProperty(Required = Required.Always)]
        public string ContainerId { get; set; }
        [JsonProperty(Required = Required.Always)]
        public string NewName { get; set; }
    }
}