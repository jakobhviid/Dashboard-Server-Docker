using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace DashboardServer.CommandServer.ContainerRequests
{
    [JsonConverter(typeof(StringEnumConverter))]  
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