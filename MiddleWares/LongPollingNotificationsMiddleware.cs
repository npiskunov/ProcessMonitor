using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using ProcessMonitorWeb.Services;
using System.Text.Json;
using System.Threading.Tasks;

namespace ProcessMonitorWeb.MiddleWares
{
    public class LongPollingNotificationsMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IMetricsService _service;
        private readonly JsonSerializerOptions _serializerOptions;
        private readonly ILogger _logger;

        public LongPollingNotificationsMiddleware(RequestDelegate next, IMetricsService service, JsonSerializerOptions serializerOptions, ILogger<LongPollingNotificationsMiddleware> logger)
        {
            _next = next;
            _service = service;
            _serializerOptions = serializerOptions;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext ctx)
        {
            var alert = await _service.WaitAlert();
            _logger.LogInformation("Performance alert response ready to be sent");
            await ctx.Response.WriteAsync(JsonSerializer.Serialize(alert, _serializerOptions));
        }
    }
}
