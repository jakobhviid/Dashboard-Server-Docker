using System;
using System.Threading;
using Confluent.Kafka;
using System.Text.Json;
using System.Text.Json.Serialization;
using KafkaClasses;

namespace CommandServer
{
    class Program
    {
        static void Main(string[] args)
        {
            var consumerConfig = new ConsumerConfig
            {
                GroupId = "command-server-group",
                BootstrapServers = "kafka1.cfei.dk:9092,kafka2.cfei.dk:9092,kafka3.cfei.dk:9092", // TODO - servers is to be replaced by the environment variable KAFKA_URLS
                AutoOffsetReset = AutoOffsetReset.Earliest,

            };

            Console.WriteLine(JsonSerializer.Serialize(new ContainerRequest
            {
                Action = ContainerAction.RENAME,
                JsonParameterString = JsonSerializer.Serialize(new RenameContainerParameter
                {
                    ContainerId = "34a2aa0aca",
                    NewName = "MyName"
                })
            }));

            using (var c = new ConsumerBuilder<Ignore, string>(consumerConfig).Build())
            {

                // TODO - 'container1' is to be replaced by the server host name environment variable
                var requestTopic = "container1-commands-requests";
                c.Subscribe(requestTopic);

                Console.WriteLine($"Listening for commands on topic {requestTopic}");

                CancellationTokenSource cts = new CancellationTokenSource();
                Console.CancelKeyPress += (_, e) =>
                {
                    // when CTRL C is pressed, terminate the process. Otherwise keep it going
                    e.Cancel = true;
                    cts.Cancel();
                };

                try
                {
                    while (true)
                    {
                        try
                        {
                            var consumeResult = c.Consume(cts.Token); // Polling for new messages, waiting here until message recieved

                            var messageJsonString = consumeResult.Message.Value;

                            ContainerRequest request = JsonSerializer.Deserialize<ContainerRequest>(messageJsonString);
                            CallAction(request.Action, request.JsonParameterString); // Call the method
                        }
                        catch (ConsumeException ex)
                        {
                            Console.Error.WriteLine(ex.Error);
                        }
                        catch (JsonException)
                        {
                            Console.WriteLine("Invalid request json format");
                        }
                    }
                }
                catch (OperationCanceledException)
                { }
                finally
                {
                    // Ensure the consumer leaves the group cleanly and final offsets are committed.
                    c.Close();
                }

            }
        }

        private static void CallAction(ContainerAction action, string jsonParameterString)
        {
            var producerConfig = new ProducerConfig { BootstrapServers = "kafka1.cfei.dk:9092,kafka2.cfei.dk:9092,kafka3.cfei.dk:9092" };
            using (var p = new ProducerBuilder<Null, string>(producerConfig).Build())
            {
                switch (action)
                {
                    case ContainerAction.RENAME:
                        var parameters = JsonSerializer.Deserialize<RenameContainerParameter>(jsonParameterString);
                        Actions.RenameContainer(parameters, p);
                        break;
                    default:
                        Console.WriteLine("Whaat");
                        break;
                }
            }
        }
    }
}
