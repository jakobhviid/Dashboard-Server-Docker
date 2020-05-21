using System;
using Newtonsoft.Json;

namespace DashboardServer.Updaters.UpdaterResponses
{
    public struct OverviewContainerData
    {
        [JsonProperty(Required = Required.Always)]
        public string Id { get; set; }

        [JsonProperty(Required = Required.Always)]
        public string Name { get; set; }

        [JsonProperty(Required = Required.Always)]
        public string Image { get; set; }

        [JsonProperty(Required = Required.Always)]
        public string State { get; set; }

        [JsonProperty(Required = Required.Always)]
        public string Status { get; set; }

        [JsonProperty(Required = Required.Always)]
        public DateTime CreationTime { get; set; }

        [JsonProperty(Required = Required.Always)]
        public DateTime UpdateTime { get; set; }
    }
}
