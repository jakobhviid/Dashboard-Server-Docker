using System;
using System.Collections.Generic;
using Docker.DotNet.Models;
using Newtonsoft.Json;

namespace DashboardServer.CommandServer.ContainerRequests
{
    public struct RunNewContainerParameters
    {
        [JsonProperty(Required = Required.Always)]
        public ContainerActionType Action { get; set; }
        [JsonProperty(Required = Required.Always)]
        public string Image { get; set; }
        [JsonProperty(Required = Required.AllowNull)]
        public string Name { get; set; }
        [JsonProperty(Required = Required.AllowNull)]
        public IList<string> Command { get; set; }
        [JsonProperty(Required = Required.AllowNull)]
        public IDictionary<string, IList<PortBinding>> Ports { get; set; } // TODO: Check if PortBinding can be serialised, if not create your own and manually map it
        [JsonProperty(Required = Required.AllowNull)]
        public IList<string> Environment { get; set; }
        [JsonProperty(Required = Required.AllowNull)]
        public RestartPolicy RestartPolicy { get; set; } // TODO: Check if PortBinding can be serialised, if not create your own and manually map it
        [JsonProperty(Required = Required.AllowNull)]
        public IDictionary<string, EmptyStruct> Volumes { get; set; }
        [JsonProperty(Required = Required.AllowNull)]
        public IList<string> VolumesFrom { get; set; }
        //TODO: Add network
    }
}