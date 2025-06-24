namespace Fleet.Blazor;

public static class BlazorHostBuilder
{
    public static IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
        .ConfigureWebHostDefaults(webBuilder =>
        {
            webBuilder.UseStartup<Startup>();
            webBuilder.ConfigureKestrel(options =>
            {
                options.ListenLocalhost(5001, listenOptions => listenOptions.UseHttps());
            });
        });
}
