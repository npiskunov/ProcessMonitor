using ProcessMonitorWeb.Contract;
using System;
using System.Threading.Tasks;

namespace ProcessMonitorWeb.Services
{
    public interface IMetricsService
    {
        event Func<ProcessMetricDto, Task> MetricsScrappedAsync;
        void BroadCastMetric(ProcessMetricDto metric);
        void BroadCastAlert(ResourceAlertDto alert);
        Task<ResourceAlertDto> WaitAlert();
        void ClearAllSubscriptions();
    }
}
