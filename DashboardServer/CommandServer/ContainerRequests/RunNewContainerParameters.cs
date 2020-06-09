using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace DashboardServer.CommandServer.ContainerRequests
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ContainerRestartPolicy
    {
        [EnumMember(Value = "always")]
        Always, [EnumMember(Value = "onFailure")]
        OnFailure, [EnumMember(Value = "unlessStopped")]
        UnlessStopped, [EnumMember(Value = "none")]
        None
    }

    public class ContainerRestart
    {
        [JsonProperty(Required = Required.Always)]
        public ContainerRestartPolicy RestartPolicy { get; set; }

        [JsonProperty(Required = Required.Default)]
        public int? MaximumRetryCount { get; set; }
    }

    public class ContainerPortBinding
    {
        [JsonProperty(Required = Required.Always)]
        public string ContainerPort { get; set; }

        [JsonProperty(Required = Required.Always)]
        public string HostPort { get; set; }
    }

    public class ContainerEnvironmentEntry
    {
        [JsonProperty(Required = Required.Always)]
        public string Key { get; set; }

        [JsonProperty(Required = Required.Always)]
        public string Value { get; set; }
    }
    public class ContainerVolumeEntry
    {
        [JsonProperty(Required = Required.Always)]
        public string HostPath { get; set; }

        [JsonProperty(Required = Required.Always)]
        public string ContainerPath { get; set; }
    }

    public class RunNewContainerParameters
    {
        public ContainerActionType Action = ContainerActionType.RUN_NEW;
        [JsonProperty(Required = Required.Always)]
        public string Image { get; set; }

        [JsonProperty(Required = Required.Default)]
        public string Name { get; set; }

        [JsonProperty(Required = Required.Default)]
        public string Command { get; set; }

        [JsonProperty(Required = Required.Default)]
        public IList<ContainerPortBinding> Ports { get; set; }

        [JsonProperty(Required = Required.Default)]
        public IList<ContainerEnvironmentEntry> Environment { get; set; }

        [JsonProperty(Required = Required.Default)]
        public ContainerRestart RestartPolicy { get; set; }

        [JsonProperty(Required = Required.Default)]
        public IList<ContainerVolumeEntry> Volumes { get; set; }

        [JsonProperty(Required = Required.Default)]
        public IList<string> VolumesFrom { get; set; }

        [JsonProperty(Required = Required.Default)]
        public string NetworkMode { get; set; }
        //TODO: Add network
    }
}
