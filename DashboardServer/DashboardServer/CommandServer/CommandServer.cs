using System;
using System.Threading;
using Confluent.Kafka;
using DashboardServer.CommandServer.ContainerRequests;
using DashboardServer.CommandServer.ContainerResponses;
using Newtonsoft.Json;

namespace DashboardServer.CommandServer {
    public class CommandServer {
        private const string bootstrapServers = "kafka1.cfei.dk:9092,kafka2.cfei.dk:9092,kafka3.cfei.dk:9092"; // TODO: servers is to be replaced by the environment variable KAFKA_URLS
        private static readonly string responseTopic = "container1-commands-responses"; // TODO: 'container1' is to be replaced by the server host name environment variable
        private static readonly string requestTopic = "container1-commands-requests"; // TODO: 'container1' is to be replaced by the server host name environment variable

        public static void Start (CancellationTokenSource cts) {
            var consumerConfig = new ConsumerConfig {
                GroupId = "command-server-group",
                BootstrapServers = bootstrapServers,
                AutoOffsetReset = AutoOffsetReset.Earliest,
            };

            using (var c = new ConsumerBuilder<Ignore, string> (consumerConfig).Build ()) {

                c.Subscribe (requestTopic);

                Console.WriteLine ($"Listening for commands on topic {requestTopic}");

                try {
                    var producerConfig = new ProducerConfig { BootstrapServers = bootstrapServers };
                    using (var p = new ProducerBuilder<Null, string> (producerConfig).Build ()) {
                        while (true) {
                            try {
                                var consumeResult = c.Consume (cts.Token); // Polling for new messages, waiting here until message recieved

                                var messageJsonString = consumeResult.Message.Value;

                                ContainerRequest request = JsonConvert.DeserializeObject<ContainerRequest> (messageJsonString);
                                CallAction (request.Action, messageJsonString, p); // Call the method

                            } catch (ConsumeException ex) {
                                Console.Error.WriteLine (ex.Error);
                            } catch (Newtonsoft.Json.JsonException ex) {
                                p.Produce (responseTopic, new Message<Null, string> {
                                    Value = JsonConvert.SerializeObject (new ContainerResponse {
                                        ResponseStatusCode = 400,
                                            Message = ex.Message
                                    })
                                });
                            }
                        }
                    }
                } catch (OperationCanceledException) { } finally {
                    // Ensure the consumer leaves the group cleanly and final offsets are committed.
                    c.Close ();
                }

            }
        }

        private static void CallAction (ContainerActionType action, string jsonParameterString, IProducer<Null, String> p) {
            switch (action) {
                case ContainerActionType.RUN_NEW:
                    var runNewParam = JsonConvert.DeserializeObject<RunNewContainerParameters> (jsonParameterString);
                    ContainerAction.RunNewContainer (runNewParam, p);
                    break;
                case ContainerActionType.RENAME:
                    var parameters = JsonConvert.DeserializeObject<RenameContainerParameter> (jsonParameterString);
                    ContainerAction.RenameContainer (parameters, p);
                    break;
                default:
                    throw new ArgumentException ("Action not supported");
            }
        }
    }
}