using DotNetEnv;
using Fleet.Blazor.Components;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fleet.Blazor;

public class Startup
{
    private readonly IConfiguration _configuration;

    public Startup(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        // migrate all your builder.Services.AddXyz(...) calls here
        //services.AddSerilog();
        Console.WriteLine($"Configuring services in Startup.cs {_configuration}");
        var corsExemption = _configuration["CORS_EXCEMPTION"] ?? throw new InvalidOperationException("CORS_EXCEMPTION is missing");
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy
                  // *exactly* your extension origin, no "@temporary-addon"
                  .WithOrigins(corsExemption)
                  .AllowAnyHeader()
                  .AllowAnyMethod();
            });
        });
        // services.AddControllers();
        // Add services to the container.
        services.AddRazorComponents()
            .AddInteractiveServerComponents();
        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
            });

        // ... razor pages, DI registrations, etc.
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        Console.WriteLine($"Environment: {env.EnvironmentName}");
        StaticWebAssetsLoader.UseStaticWebAssets(env, app.ApplicationServices.GetRequiredService<IConfiguration>());
        //Console.WriteLine($"WebRootPath: {env.WebRootPath}");
        //foreach (var file in Directory.GetFiles(env.WebRootPath))
        //{
        //    Console.WriteLine($"Found file: {Path.GetFileName(file)}");
        //}
        app.UseSerilogRequestLogging(); // Add Serilog request logging
        if (!env.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }
        app.UseStaticFiles();
        //app.UseStaticFiles(new StaticFileOptions
        //{
        //    FileProvider = new PhysicalFileProvider(env.WebRootPath)
        //});
        //app.UseStaticFiles("/wwwroot"); // Use default static file serving

        app.UseHttpsRedirection();
        app.UseRouting();

        app.UseCors();
        app.UseAuthorization();
        app.UseAntiforgery();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapStaticAssets();
            endpoints.MapControllers();
            //endpoints.MapGet("/api/status", () => "Fleet Web Server Running!");
            endpoints.MapRazorComponents<App>().AddInteractiveServerRenderMode(); // Moved to endpoints
            //endpoints.MapBlazorHub();
        });
    }
}
