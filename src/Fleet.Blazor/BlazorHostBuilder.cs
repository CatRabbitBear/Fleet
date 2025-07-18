using Serilog;

namespace Fleet.Blazor;

public static class BlazorHostBuilder
{
    public static IHostBuilder CreateHostBuilder(string[] args)
    {
        var devArgs = new[] { "--environment", "Development" };
        return Host.CreateDefaultBuilder([..args, ..devArgs ])
             .UseSerilog((ctx, lc) => lc
                .ReadFrom.Configuration(ctx.Configuration)
                .Enrich.FromLogContext())
             .ConfigureWebHostDefaults(webBuilder =>
             {
                 // webBuilder.UseWebRoot(webRootPath);
                 webBuilder.UseStaticWebAssets();

                 webBuilder.UseStartup<Startup>();
                 webBuilder.ConfigureKestrel(options =>
                         {
                    options.ListenLocalhost(5001, listenOptions => listenOptions.UseHttps());
                });
                 webBuilder.ConfigureServices(services =>
                         {
                             // Refactor into IHttpFactory instead. 
                            services.AddSingleton(sp =>
                                new HttpClient
                                {
                                    BaseAddress = new Uri(webBuilder.GetSetting("BaseAddress") ?? "https://localhost:5001")
                                });
                });
             });
    }
}
