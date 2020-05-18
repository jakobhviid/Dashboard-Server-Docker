using Newtonsoft.Json;

namespace DashboardServer.CommandServer.ContainerRequests
{
    public enum ContainerActionType // TODO: string representation
    {
        RUN_NEW,
        START,
        STOP,
        REMOVE,
        RESTART,
        RENAME,
        UPDATE_CONFIGURATION
    }
    public struct ContainerRequest
    {
        [JsonProperty(Required = Required.Always)]
        public ContainerActionType Action { get; set; }
    }
}