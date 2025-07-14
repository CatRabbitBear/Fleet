using DotNetEnv;
using Fleet.Blazor.Components;
using Fleet.Blazor.PluginSystem;
using Fleet.Blazor.PluginSystem.Interfaces;
using Fleet.Blazor.SQLite;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel;
using Serilog;
using System.Text.Json;
using System.Text.Json.Serialization;
using YamlDotNet.Core.Tokens;

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

        // DB
        // Make sure %LocalAppData%/Sleepr exists
        // This is where the SQLite database will be stored
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Sleepr");
        Directory.CreateDirectory(appDataPath);

        // Configure SQLite handlers
        services.AddScoped<SqliteMcpHandler>((sp) =>
        {
            var dbFilePath = Path.Combine(appDataPath,
                _configuration["PluginsDb:Path"] ?? "plugins.db");
            var connectionString = $"Data Source={dbFilePath}";
            return new SqliteMcpHandler(connectionString);
        });

        services.AddScoped<SqliteAgentOutputHandler>((sp) =>
        {
            var dbFilePath = Path.Combine(appDataPath,
                _configuration["OutputDb:Path"] ?? "agents-output.db");
            var connectionString = $"Data Source={dbFilePath}";
            return new SqliteAgentOutputHandler(connectionString);
        });

        // DB + MCP
        services.AddScoped<IMcpRepoManager, McpJsonRepoManager>();
        services.AddScoped<IPluginManager>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<McpPluginManager>>();
            var repo = sp.GetRequiredService<IMcpRepoManager>();
            return new McpPluginManager(repo, logger);
        });


        // services.AddControllers();
        // Add services to the container.
#pragma warning disable SKEXP0070
        services.AddAzureAIInferenceChatCompletion(
            endpoint: new Uri(Environment.GetEnvironmentVariable("AZURE_ENDPOINT")!),
            modelId: Environment.GetEnvironmentVariable("AZURE_MODEL_ID")!,
            apiKey: _configuration["AZURE_MISTRAL_NEMO_KEY"] ?? throw new InvalidOperationException("AZURE_MISTRAL_NEMO_KEY is missing")
        );

        services.AddTransient((serviceProvider) =>
        {
            return new Kernel(serviceProvider);
        });


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
