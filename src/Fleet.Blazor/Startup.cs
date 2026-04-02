using DotNetEnv;
using Fleet.Blazor.Agents;
using Fleet.Blazor.Components;
using Fleet.Blazor.Pipeline;
using Fleet.Blazor.Pipeline.Interfaces;
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
        var corsExemption = _configuration["FLEET_CORS_EXCEMPTION"];
        var corsOrigins = (corsExemption ?? string.Empty)
            .Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(origin => !string.IsNullOrWhiteSpace(origin))
            .ToArray();

        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                if (corsOrigins.Length > 0)
                {
                    policy
                        // *exactly* your extension origin(s), no "@temporary-addon"
                        .WithOrigins(corsOrigins)
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                    return;
                }

                // No explicit CORS origin configured. Same-origin requests continue to work,
                // and cross-origin requests are denied by default.
                policy
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        // DB
        // Make sure %LocalAppData%/Sleepr exists
        // This is where the SQLite database will be stored
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Fleet");
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
        services.AddScoped<McpPluginManager>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<McpPluginManager>>();
            var repo = sp.GetRequiredService<IMcpRepoManager>();
            return new McpPluginManager(repo, logger);
        });


        // services.AddControllers();
        // Add services to the container.
        var endpoint = ResolveRequiredSetting("FLEET_AZURE_OPENAI_ENDPOINT", "FLEET_AZURE_ENDPOINT");
        var deployment = ResolveRequiredSetting("FLEET_AZURE_OPENAI_DEPLOYMENT", "FLEET_AZURE_MODEL_ID");
        var apiKey = ResolveRequiredSetting("FLEET_AZURE_OPENAI_API_KEY", "FLEET_AZURE_MODEL_KEY");

        services.AddAzureOpenAIChatCompletion(
            deploymentName: deployment,
            endpoint: endpoint,
            apiKey: apiKey
        );

        services.AddTransient((serviceProvider) =>
        {
            return new Kernel(serviceProvider);
        });
        services.AddScoped<IPipelineContextFactory, PipelineContextFactory>();
        services.AddScoped<ChatCompletionsRunner>();

        services.AddHttpClient();

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
        // app.UseStaticFiles();
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

    private string ResolveRequiredSetting(params string[] keys)
    {
        foreach (var key in keys)
        {
            var value = _configuration[key];
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        throw new InvalidOperationException(
            $"Missing required setting. Provide one of: {string.Join(", ", keys)}");
    }
}
