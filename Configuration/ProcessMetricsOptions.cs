using System;

namespace ProcessMonitorWeb.Configuration
{
    public class ProcessMetricsOptions
    {
        public int MaxProcesses { get; set; } = int.MaxValue;
        public TimeSpan ScrappingInterval { get; set; } = TimeSpan.FromSeconds(1);
        public float CpuPercentAlertThreshold { get; set; } = 50F; //if CPU load > 50%
        public float AvailableMemoryMbAlertThreshold { get; set; } = 1024F; // if mem < 1Gb
        public float DiskAlertThreshold { get; set; } = 50F; //if disk busy > 50% time
    }
}
