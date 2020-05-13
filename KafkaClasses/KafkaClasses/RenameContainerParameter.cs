using System;
namespace KafkaClasses
{
    public class RenameContainerParameter : RequestParamater
    {
        public string ContainerId { get; set; }
        public string NewName { get; set; }
    }
}
