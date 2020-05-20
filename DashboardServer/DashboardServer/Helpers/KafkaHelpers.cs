using System;
using System.Net;
using System.Threading.Tasks;
using Confluent.Kafka;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace DashboardServer.Helpers
{
    public class KafkaHelpers
    {
        public static string bootstrapServers = Environment.GetEnvironmentVariable("KAFKA_URL") ?? "kafka1.cfei.dk:9092,kafka2.cfei.dk:9092,kafka3.cfei.dk:9092";
        public static string servername = Environment.GetEnvironmentVariable("SERVER_NAME") ?? "PlaceholderServer";
        public static string selfContainerId = Dns.GetHostName()[..10];
        public static readonly string requestTopic = servername + "-" + selfContainerId + "-command-requests"; // servername plus this specific container id + command-requests
        public static readonly string responseTopic = servername + "-" + selfContainerId + "-command-requests";

        private static JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        public static async Task SendMessageAsync(string topic, object messageToSerialize, IProducer<Null, string> p)
        {
            try
            {
                var deliveryReport = await p.ProduceAsync(
                    topic, new Message<Null, string> { Value = JsonConvert.SerializeObject(messageToSerialize, _jsonSettings) });

                Console.WriteLine($"delivered to: {deliveryReport.TopicPartitionOffset}");
            }
            catch (ProduceException<string, string> e)
            {
                Console.WriteLine($"Failed to deliver message: {e.Message} [{e.Error.Code}]");
            }
        }
    }
}
