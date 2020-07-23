using System;
using System.Net;
using System.Threading.Tasks;
using Confluent.Kafka;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace DashboardServer.Helpers {
    public class KafkaHelpers {
        public static string BootstrapServers = Environment.GetEnvironmentVariable("DASHBOARDS_KAFKA_URL") ?? "kafka1.cfei.dk:9092,kafka2.cfei.dk:9092,kafka3.cfei.dk:9092";
        public static string Servername = Environment.GetEnvironmentVariable("DASHBOARDS_SERVER_NAME") ?? "PlaceholderServer";
        public static string SelfContainerId = Dns.GetHostName()[..10];
        public static readonly string RequestTopic = $"command-requests-{Servername}-{SelfContainerId}";
        public static readonly string ResponseTopic = $"command-responses-{Servername}-{SelfContainerId}";

        private static JsonSerializerSettings _jsonSettings = new JsonSerializerSettings {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        public static async Task SendMessageAsync(string topic, object messageToSerialize, IProducer<Null, string> p) {
            try {
                var deliveryReport = await p.ProduceAsync(
                    topic, new Message<Null, string> { Value = JsonConvert.SerializeObject(messageToSerialize, _jsonSettings) });

                Console.WriteLine($"delivered to: {deliveryReport.TopicPartitionOffset}");
            } catch (ProduceException<string, string> e) {
                Console.WriteLine($"Failed to deliver message: {e.Message} [{e.Error.Code}]");
            } catch (InvalidOperationException e) {
                Console.WriteLine($"Error {e.Message}");
            } catch (JsonSerializationException e) {
                Console.WriteLine($"Error serializing {e.Message}");
            }
        }
    }
}
