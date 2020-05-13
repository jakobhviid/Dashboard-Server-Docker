using System.Text.RegularExpressions;
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
            var safeContainerId = Regex.Escape(parameter.ContainerId.Replace(" ", String.Empty));
            var safeNewName = Regex.Escape(parameter.NewName.Replace(" ", String.Empty));
            var output = $"../../scripts/rename-container.py {safeContainerId} {safeNewName}".Bash();
            if (output.Item1 == 1) // renaming was not successful
            {
                producer.ProduceAsync(responseTopic, new Message<Null, string> { Value = output.Item2 });
            }
        }
    }
}