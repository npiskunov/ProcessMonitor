using ProcessMonitorWeb.Contract;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ProcessMonitorWeb.Services
{
    public class MetricsService : IMetricsService
    {
        public event Func<ProcessMetricDto, Task> MetricsScrappedAsync;
        private TaskCompletionSource<ResourceAlertDto> _alertCompletion = new TaskCompletionSource<ResourceAlertDto>();
        public void BroadCastAlert(ResourceAlertDto alert)
        {
            if (alert == null) throw new ArgumentNullException(nameof(alert));
            _alertCompletion.SetResult(alert);
            _alertCompletion = new TaskCompletionSource<ResourceAlertDto>();
        }

        public void BroadCastMetric(ProcessMetricDto metric)
        {
            if (MetricsScrappedAsync != default)
            {
                MetricsScrappedAsync(metric);
            }
        }

        public void ClearAllSubscriptions()
        {
            if (MetricsScrappedAsync == default) return;
            foreach (var item in MetricsScrappedAsync.GetInvocationList().Cast<Func<ProcessMetricDto, Task>>())
            {
                MetricsScrappedAsync -= item;
            }
        }

        public Task<ResourceAlertDto> WaitAlert()
        {
            return _alertCompletion.Task;
        }
    }
}
