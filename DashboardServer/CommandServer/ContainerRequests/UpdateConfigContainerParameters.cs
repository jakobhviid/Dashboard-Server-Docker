using Newtonsoft.Json;

namespace DashboardServer.CommandServer.ContainerRequests
{
    public class UpdateConfigContainerParameters
    {
        [JsonProperty(Required = Required.Always)]
        public const ContainerActionType Action = ContainerActionType.UPDATE_CONFIGURATION;

        [JsonProperty(Required = Required.Always)]
        public string ContainerId { get; set; }

        [JsonProperty(Required = Required.AllowNull)]
        public ushort BlkioWeight { get; set; }

        [JsonProperty(Required = Required.AllowNull)]
        public int MyProperty { get; set; }

        [JsonProperty(Required = Required.AllowNull)]
        public long MemoryLimit { get; set; }

        [JsonProperty(Required = Required.AllowNull)]
        public long MemoryParameters { get; set; }

        [JsonProperty(Required = Required.AllowNull)]
        public long MemorySwapLimit { get; set; }

        [JsonProperty(Required = Required.AllowNull)]
        public long KernelMemory { get; set; }

        // [JsonProperty(Required = Required.AllowNull)]
        // public RestartPolicy RestartPolicy { get; set; } TODO:

        [JsonProperty(Required = Required.AllowNull)]
        public long CPUShares { get; set; }

        [JsonProperty(Required = Required.AllowNull)]
        public long CPUPeriod { get; set; }

        [JsonProperty(Required = Required.AllowNull)]
        public string CPUSetCPUs { get; set; }

        [JsonProperty(Required = Required.AllowNull)]
        public string CPUSetMems { get; set; }
    }
}
