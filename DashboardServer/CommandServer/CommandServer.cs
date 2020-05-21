using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using DashboardServer.CommandServer.ContainerRequests;
using DashboardServer.CommandServer.ContainerResponses;
using DashboardServer.CommandServer.Contracts;
using DashboardServer.Helpers;
using Newtonsoft.Json;

namespace DashboardServer.CommandServer
{
    public class CommandServer
    {
        public static async void Start(CancellationTokenSource cts)
        {
            var consumerConfig = new ConsumerConfig
            {
                GroupId = "command-server",
                BootstrapServers = KafkaHelpers.BootstrapServers,
                AutoOffsetReset = AutoOffsetReset.Earliest,
            };

            using(var c = new ConsumerBuilder<Ignore, string>(consumerConfig).Build())
            {

                c.Subscribe(KafkaHelpers.RequestTopic);

                Console.WriteLine($"Listening for commands on topic {KafkaHelpers.RequestTopic}");
                try
                {
                    var producerConfig = new ProducerConfig { BootstrapServers = KafkaHelpers.BootstrapServers, Acks = Acks.Leader };
                    using(var p = new ProducerBuilder<Null, string>(producerConfig).Build())
                    {
                        while (true)
                        {
                            try
                            {
                                var consumeResult = c.Consume(cts.Token); // Polling for new messages, waiting here until message recieved

                                var messageJsonString = consumeResult.Message.Value;

                                ContainerRequest request = JsonConvert.DeserializeObject<ContainerRequest>(messageJsonString);
                                await CallAction(request.Action, messageJsonString, p);

                            }
                            catch (ConsumeException ex)
                            {
                                Console.Error.WriteLine(ex.Error);
                            }
                            catch (Newtonsoft.Json.JsonException ex)
                            {
                                p.Produce(KafkaHelpers.ResponseTopic, new Message<Null, string>
                                {
                                    Value = JsonConvert.SerializeObject(new ContainerResponse
                                    {
                                        ResponseStatusCode = 400,
                                            Message = ex.Message
                                    })
                                });
                            }
                        }
                    }
                }
                catch (OperationCanceledException) { }
                finally
                {
                    // Ensure the consumer leaves the group cleanly and final offsets are committed.
                    c.Close();
                }

            }
        }

        private async static Task CallAction(ContainerActionType action, string jsonParameterString, IProducer<Null, String> p)
        {
            switch (action)
            {
                case ContainerActionType.RUN_NEW:
                    var runNewParam = JsonConvert.DeserializeObject<RunNewContainerParameters>(jsonParameterString);
                    await ContainerAction.RunNewContainer(runNewParam, p);
                    break;
                case ContainerActionType.START:
                    var startParam = JsonConvert.DeserializeObject<StartContainerParameters>(jsonParameterString);
                    await ContainerAction.StartContainer(startParam, p);
                    break;
                case ContainerActionType.STOP:
                    var stopParam = JsonConvert.DeserializeObject<StopContainerParameters>(jsonParameterString);
                    await ContainerAction.StopContainer(stopParam, p);
                    break;
                case ContainerActionType.REMOVE:
                    var removeParam = JsonConvert.DeserializeObject<RemoveContainerParameters>(jsonParameterString);
                    await ContainerAction.RemoveContainer(removeParam, p);
                    break;
                case ContainerActionType.RESTART:
                    var restartParam = JsonConvert.DeserializeObject<RestartContainerParameters>(jsonParameterString);
                    await ContainerAction.RestartContainer(restartParam, p);
                    break;
                case ContainerActionType.RENAME:
                    var parameters = JsonConvert.DeserializeObject<RenameContainerParameter>(jsonParameterString);
                    await ContainerAction.RenameContainer(parameters, p);
                    break;
                case ContainerActionType.UPDATE_CONFIGURATION:
                    var updateParam = JsonConvert.DeserializeObject<UpdateConfigContainerParameters>(jsonParameterString);
                    await ContainerAction.UpdateConfigContainer(updateParam, p);
                    break;
                default:
                    await KafkaHelpers.SendMessageAsync(KafkaHelpers.ResponseTopic, new ContainerResponse {ResponseStatusCode = 404, Message = ResponseMessageContracts.METHOD_CALL_NOT_VIABLE}, p);
                    break;
            }
        }
    }
}
