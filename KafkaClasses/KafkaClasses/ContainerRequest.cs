using System;

namespace KafkaClasses
{
    public enum ContainerAction
    {
        RUN_NEW,
        START,
        STOP,
        REMOVE,
        RESTART,
        RENAME,
        UPDATE_CONFIGURATION
    }

    public class RequestParamater {}
    public class ContainerRequest
    {
        public ContainerAction Action { get; set; }
        public string JsonParameterString { get; set; }
    }
}
