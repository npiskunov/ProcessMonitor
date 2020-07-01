
using System.ComponentModel;

namespace ProcessMonitorWeb.Contract
{
    public class ResourceAlertDto
    {
        [Description("Machine resource type causing alert")]
        public ResourceType Type { get; set; }

        [Description("Server threshold value set for alert type")]
        public float Threshold { get; set; }

        [Description("Actual value of performance counter which violates the threshold")]
        public float Value { get; set; }
    }

    public enum ResourceType
    {
        Cpu = 0,
        Memory = 1,
        Disk = 2
    }
}
