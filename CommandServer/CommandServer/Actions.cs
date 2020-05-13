using KafkaClasses;
using System;
using Confluent.Kafka;

namespace CommandServer
{
    public class Actions
    {
        private const string responseTopic = "container1-commands-responses"; // TODO - replace container1 with environment variable
        public static void RenameContainer(RenameContainerParameter parameter, IProducer<Null, string> producer)
        {
            var output = $"../../scripts/rename-container.py {parameter.ContainerId.Replace(" ", String.Empty)} {parameter.NewName.Replace(" ", String.Empty)}".Bash();
            if (output.Item1 == 1) // renaming was not successfull
            {
                producer.ProduceAsync(responseTopic, new Message<Null, string> { Value = output.Item2 });
            }
        }
    }
}