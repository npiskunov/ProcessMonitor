
using System.ComponentModel;

namespace ProcessMonitorWeb.Contract
{
    public class ProcessMetricDto
    {
        [Description("Indicates how to treat metric")]
        public Operation Operation { get; set; }

        [Description("Metrics value")]
        public Payload Payload { get; set; }

        public ProcessMetricDto(Payload payload)
        {
            Operation = Operation.Metric;
            Payload = payload;
        }

        public ProcessMetricDto(string name, Operation operation = Operation.Metric)
        {
            Operation = operation;
            Payload = new Payload() { Name = name };
        }
    }

    public class Payload
    {
        [Description("Performance counter name for particular process")]
        public string Name { get; set; }

        [Description("Total CPU % consumed by process")]
        public float Cpu { get; set; }

        [Description("Total physical memory consuned (bytes)")]
        public float Memory { get; set; }

        [Description("Total IO bytes/sec including disk, network, etc")]
        public float Io { get; set; }
    }

    public enum Operation
    {
        Metric = 0,
        ProcessTerminated
    }
}
