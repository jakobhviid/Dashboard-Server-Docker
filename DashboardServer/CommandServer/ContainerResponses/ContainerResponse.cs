using System.Collections.Generic;
using Newtonsoft.Json;

namespace DashboardServer.CommandServer.ContainerResponses
{
    public class ContainerResponse
    {
        [JsonProperty(Required = Required.Always)]
        public int ResponseStatusCode { get; set; }

        [JsonProperty(Required = Required.Always)]
        public string Message { get; set; }

        [JsonProperty(Required = Required.AllowNull)]
        public string[] ContainerIds { get; set; }
    }
}
