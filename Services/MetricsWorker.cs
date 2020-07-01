using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProcessMonitorWeb.Configuration;
using ProcessMonitorWeb.Contract;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ProcessMonitorWeb.Services
{
    public class MetricsWorker : BackgroundService
    {
        private readonly ILogger<MetricsWorker> _logger;
        private readonly IMetricsService _metricsService;
        private readonly ProcessMetricsOptions _opt;
        private readonly Dictionary<string, (PerformanceCounter Cpu, PerformanceCounter Memory, PerformanceCounter Io)> _processesCounters;
        private readonly Dictionary<ResourceType, PerformanceCounter> _totalCounters;

        public MetricsWorker(ILogger<MetricsWorker> logger, IOptions<ProcessMetricsOptions> opt, IMetricsService metricsService)
        {
            _logger = logger;
            _metricsService = metricsService;
            _opt = opt.Value;
            _processesCounters = new PerformanceCounterCategory("Process")
                .GetInstanceNames()
                .Take(_opt.MaxProcesses)
                .Select(_ => (Cpu : new PerformanceCounter("Process", "% Processor Time", _), 
                    Memory : new PerformanceCounter("Process", "Working Set", _), 
                    Io : new PerformanceCounter("Process", "IO Data Bytes/sec", _)))
                .ToDictionary(_ => _.Cpu.InstanceName);
            _totalCounters = new Dictionary<ResourceType, PerformanceCounter>() {
                {ResourceType.Cpu, new PerformanceCounter("Processor", "% Processor time", "_Total") },
                {ResourceType.Memory, new PerformanceCounter("Memory", "Available MBytes") },
                {ResourceType.Disk, new PerformanceCounter("PhysicalDisk", "% Disk Time", "_Total") }
            };
        }
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation($"Started periodic metrics scrapping. Max. monitored processes: {_opt.MaxProcesses} Scrapping interval: {_opt.ScrappingInterval}");

            var metricsScrapping = Task.Run(() => RunByInterval(ScrapMetrics), stoppingToken);
            var alertsScrapping = Task.Run(() => RunByInterval(ScrapAlerts), stoppingToken);

            return Task.WhenAll(metricsScrapping, alertsScrapping);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Trying to stop gracefully");
            _metricsService.ClearAllSubscriptions();
            await base.StopAsync(cancellationToken);
            _logger.LogInformation($"{nameof(MetricsWorker)} stopped successfully");
        }

        public override void Dispose()
        {
            base.Dispose();
            foreach (var counter in _processesCounters)
            {
                counter.Value.Cpu.Dispose();
                counter.Value.Memory.Dispose();
                counter.Value.Io.Dispose();
            }
        }

        private Task RunByInterval(Action action)
        {
            try
            {
                while (true)
                {
                    action();
                    Thread.Sleep(_opt.ScrappingInterval);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Unhadnled exception in {nameof(MetricsWorker)}");
                throw;
            }
        }

        private void ScrapAlerts()
        {
            foreach (var counter in _totalCounters)
            {
                var value = counter.Value.NextValue();
                float threshold;
                switch (counter.Key)
                {
                    case ResourceType.Cpu:
                        threshold = _opt.CpuPercentAlertThreshold;
                        break;
                    case ResourceType.Memory:
                        //for memory counter lower bound is set (based on available memory), so need to exchange value and threshold
                        threshold = value;
                        value = _opt.AvailableMemoryMbAlertThreshold;
                        break;
                    case ResourceType.Disk:
                        threshold = _opt.DiskAlertThreshold;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                
                if (value >= threshold)
                {
                    _metricsService.BroadCastAlert(new ResourceAlertDto() { Type = counter.Key, Threshold = threshold, Value =  value});
                }
            }
            Thread.Sleep(_opt.ScrappingInterval);
        }

        private void ScrapMetrics()
        {
            //parallel invocation of counters doesn't demonstrate reasonable perfromance improvment
            //Parallel.ForEach(_counters, counter => {

            //});
            foreach (var counter in _processesCounters)
            {
                try
                {
                    var metric = new ProcessMetricDto(new Payload()
                    {
                        Name = counter.Key,
                        Cpu = counter.Value.Cpu.NextValue() / Environment.ProcessorCount,
                        Memory = counter.Value.Memory.NextValue(),
                        Io = counter.Value.Io.NextValue()
                    });
                    _metricsService.BroadCastMetric(metric);
                }
                catch (InvalidOperationException e)
                {
                    _processesCounters.Remove(counter.Key);
                    _metricsService.BroadCastMetric(new ProcessMetricDto(counter.Key, Operation.ProcessTerminated));
                    _logger.LogInformation(e, $"Process {counter.Key} exited, counter removed from watch");
                }
            }
            AppendNewProcesses();
        }

        private void AppendNewProcesses()
        {
            var newProcesses = new PerformanceCounterCategory("Process")
                .GetInstanceNames()
                .Where( _ => !_processesCounters.ContainsKey(_))
                .Take(_opt.MaxProcesses - _processesCounters.Count)
                .Select(_ => (Cpu: new PerformanceCounter("Process", "% Processor Time", _), Memory: new PerformanceCounter("Process", "Working Set", _), Disk: new PerformanceCounter("Process", "IO Data Bytes/sec", _)));
            var appended = 0;
            foreach (var item in newProcesses)
            {
                _processesCounters.Add(item.Cpu.InstanceName, item);
                appended++;
            }
            _logger.LogInformation($"Added {appended} new processes to watch");
        }
    }
}
