using System;
using Newtonsoft.Json;

namespace DashboardServer.Updaters.UpdaterResponses
{
    public struct StatsContainerData
    {
        [JsonProperty(Required = Required.Always)]

        public string Id { get; set; }

        [JsonProperty(Required = Required.Always)]
        public string Name { get; set; }

        [JsonProperty(Required = Required.Always)]
        public ulong CpuUsage { get; set; }

        [JsonProperty(Required = Required.Always)]
        public int NumOfCpu { get; set; }

        [JsonProperty(Required = Required.Always)]
        public ulong SystemCpuUsage { get; set; }

        [JsonProperty(Required = Required.Always)]
        public double CpuPercentage { get; set; }

        [JsonProperty(Required = Required.Always)]
        public double MemoryPercentage { get; set; }

        [JsonProperty(Required = Required.Always)]
        public ulong NetInputBytes { get; set; }

        [JsonProperty(Required = Required.Always)]
        public ulong NetOutputBytes { get; set; }

        [JsonProperty(Required = Required.Always)]
        public ulong DiskInputBytes { get; set; }

        [JsonProperty(Required = Required.Always)]
        public ulong DiskOutputBytes { get; set; }

        [JsonProperty(Required = Required.Always)]
        public DateTime UpdateTime { get; set; }
    }
}
