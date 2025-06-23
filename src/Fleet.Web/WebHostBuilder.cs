// Sleepr.Web/WebHostBuilder.cs
using Fleet.Web;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Fleet.Web
{
    public static class WebHostBuilder
    {
        public static IHostBuilder MyCreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    // point to your Startup class, or inline minimal-API config
                    webBuilder.UseStartup<Startup>();
                    webBuilder.ConfigureKestrel(options =>
                    {
                        options.ListenLocalhost(5001, listenOptions => listenOptions.UseHttps());
                    });
                });
    }
}