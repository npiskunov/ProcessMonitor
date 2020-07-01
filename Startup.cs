using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ProcessMonitorWeb.Configuration;
using ProcessMonitorWeb.MiddleWares;
using ProcessMonitorWeb.Services;
using System.Text.Json;

namespace ProcessMonitorWeb
{
    public class Startup
    {
        public const string MetricsWsRoute = "/api/data";
        public const string NotificationsRoute = "/api/notifications";
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<ProcessMetricsOptions>(Configuration.GetSection(nameof(ProcessMetricsOptions)));
            services.Configure<HostOptions>(Configuration.GetSection(nameof(HostOptions)));
            services.AddSingleton<IMetricsService, MetricsService>();
            services.AddHostedService<MetricsWorker>();
            services.AddSingleton(new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase,  });
            services.AddRazorPages();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
            app.UseWebSockets();
            app.UseDefaultFiles();
            app.UseStaticFiles();
            //var middleWareDelegate = app.UseMiddleware<ResourceMonitorWebSocketsMiddelware>().Build();
            var middleWareDelegate = app.UseWhen(ctx => ctx.Request.Path.StartsWithSegments(MetricsWsRoute), _ => _.UseMiddleware<ResourceMonitorWebSocketsMiddelware>()).Build();
            app.UseWhen(ctx => ctx.Request.Path.Equals(NotificationsRoute), _ => _.UseMiddleware<LongPollingNotificationsMiddleware>());

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.Map(MetricsWsRoute + "/{connectionId:guid}", middleWareDelegate);
            });
        }
    }
}
