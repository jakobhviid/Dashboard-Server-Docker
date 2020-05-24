using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace DashboardServer.CommandServer.ContainerRequests
{
    public class RunNewContainerParameters
    {
        [JsonProperty(Required = Required.Always)]
        public const ContainerActionType Action = ContainerActionType.RUN_NEW;
        [JsonProperty(Required = Required.Always)]
        public string Image { get; set; }
        [JsonProperty(Required = Required.AllowNull)]
        public string Name { get; set; }
        [JsonProperty(Required = Required.AllowNull)]
        public IList<string> Command { get; set; }
        // [JsonProperty(Required = Required.AllowNull)]
        // public IDictionary<string, IList<PortBinding>> Ports { get; set; } TODO: 
        [JsonProperty(Required = Required.AllowNull)]
        public IList<string> Environment { get; set; }
        // [JsonProperty(Required = Required.AllowNull)]
        // public RestartPolicy RestartPolicy { get; set; } TODO:
        // [JsonProperty(Required = Required.AllowNull)]
        // public IDictionary<string, EmptyStruct> Volumes { get; set; } TODO:
        [JsonProperty(Required = Required.AllowNull)]
        public IList<string> VolumesFrom { get; set; }
        //TODO: Add network
    }
}