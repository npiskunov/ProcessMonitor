using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using ProcessMonitorWeb.Contract;
using ProcessMonitorWeb.Services;
using System;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ProcessMonitorWeb.MiddleWares
{
    public class ResourceMonitorWebSocketsMiddelware
    {
        private readonly RequestDelegate _next;
        private readonly IMetricsService _service;
        private readonly JsonSerializerOptions _serializerOptions;
        private readonly ILogger _logger;

        public ResourceMonitorWebSocketsMiddelware(RequestDelegate next, IMetricsService service, JsonSerializerOptions serializerOptions, ILogger<ResourceMonitorWebSocketsMiddelware> logger)
        {
            _next = next;
            _service = service;
            _serializerOptions = serializerOptions;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext ctx)
        {
            if (!ctx.WebSockets.IsWebSocketRequest)
            {
                await _next(ctx);
                return;
            }

            if (!Guid.TryParse(ctx.GetRouteValue("connectionId") as string, out var connectionId))
            {
                _logger.LogWarning($"Invalid connection ID specfied by client : {ctx.GetRouteValue("connectionId")}");
                await _next(ctx);
                return;
            }

            var s = await ctx.WebSockets.AcceptWebSocketAsync();

            async Task WriteMetric(ProcessMetricDto metric)
            {
                var bytes = Encoding.ASCII.GetBytes(JsonSerializer.Serialize(metric, _serializerOptions));
                try
                {
                    await s.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
                }
                catch (WebSocketException e)
                {
                    _logger.LogError(e, $"Client closed connection {connectionId} unexpectedly, terminating {nameof(_service.MetricsScrappedAsync)} subscription");
                    _service.MetricsScrappedAsync -= WriteMetric;
                }
            }

            _service.MetricsScrappedAsync += WriteMetric;
            try
            {
                var buffer = new byte[1024 * 4];
                var receive = await s.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                while (!receive.CloseStatus.HasValue)
                {
                    receive = await s.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                }
                _logger.LogInformation($"Client closed connection {connectionId}");
            }
            finally
            {
                _service.MetricsScrappedAsync -= WriteMetric;
            }
        }
    }
}
