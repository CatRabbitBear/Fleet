using Fleet.Blazor.Agents;
using Fleet.Blazor.Components;
using Fleet.Blazor.Pipeline;
using Fleet.Blazor.Adapters;
using Fleet.Blazor.PluginSystem;
using Fleet.Blazor.PluginSystem.Interfaces;
using Fleet.Blazor.Security;
using Fleet.Blazor.Services;
using Fleet.Blazor.SQLite;
using Fleet.Data;
using Fleet.Runtime.Adapters;
using Fleet.Runtime.Agents;
using Fleet.Runtime.Pipeline;
using Fleet.Runtime.Security;
using Fleet.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel;
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
                    policy.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod();
                    return;
                }

                policy.AllowAnyHeader().AllowAnyMethod();
            });
        });

        var appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Fleet");
        Directory.CreateDirectory(appDataPath);

        services.AddScoped<SqliteMcpHandler>(_ =>
        {
            var dbFilePath = Path.Combine(appDataPath, _configuration["PluginsDb:Path"] ?? "plugins.db");
            return new SqliteMcpHandler($"Data Source={dbFilePath}");
        });

        services.AddScoped<SqliteAgentOutputHandler>(_ =>
        {
            var dbFilePath = Path.Combine(appDataPath, _configuration["OutputDb:Path"] ?? "agents-output.db");
            return new SqliteAgentOutputHandler($"Data Source={dbFilePath}");
        });

        services.AddScoped<IMcpRepoManager, McpJsonRepoManager>();
        services.AddScoped<McpPluginManager>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<McpPluginManager>>();
            var repo = sp.GetRequiredService<IMcpRepoManager>();
            return new McpPluginManager(repo, logger);
        });

        var endpoint = ResolveRequiredSetting("FLEET_AZURE_OPENAI_ENDPOINT", "FLEET_AZURE_ENDPOINT");
        var deployment = ResolveRequiredSetting("FLEET_AZURE_OPENAI_DEPLOYMENT", "FLEET_AZURE_MODEL_ID");
        var apiKey = ResolveRequiredSetting("FLEET_AZURE_OPENAI_API_KEY", "FLEET_AZURE_MODEL_KEY");

        services.AddAzureOpenAIChatCompletion(deploymentName: deployment, endpoint: endpoint, apiKey: apiKey);

        services.AddTransient(serviceProvider => new Kernel(serviceProvider));
        services.TryAddScoped<IPluginClientAdapter, McpPluginClientAdapter>();
        services.TryAddScoped<IAgentOutputStore, SqliteAgentOutputStore>();
        services.TryAddScoped<IFileSystemHostAdapter, LocalFileSystemHostAdapter>();
        services.TryAddScoped<IProcessHostAdapter, LocalProcessHostAdapter>();
        services.AddScoped<IPipelineContextFactory, PipelineContextFactory>();
        services.AddScoped<IChatCompletionsRunner, ChatCompletionsRunner>();
        services.AddHttpClient();

        services.AddScoped<RequestIdentityContext>();
        services.AddSingleton(new LocalSessionOptions
        {
            SessionToken = _configuration["FLEET_LOCAL_SESSION_TOKEN"] ?? string.Empty
        });
        services.AddScoped<ILocalSessionValidator, LocalSessionValidator>();
        services.AddScoped<IPermissionPolicyService, PermissionPolicyService>();
        services.AddScoped<IConsentService, ConsentService>();
        services.AddScoped<IPrivilegedActionExecutor, PrivilegedActionExecutor>();
        services.TryAddScoped<IRuntimeExecutionGate, RuntimeExecutionGate>();

        services.AddSingleton<IAuditRepository>(_ =>
        {
            var dbFilePath = Path.Combine(appDataPath, _configuration["SecurityDb:Path"] ?? "security-audit.db");
            return new SqliteAuditRepository($"Data Source={dbFilePath}");
        });

        services.AddSingleton<IAgentGovernanceRepository>(_ =>
        {
            var dbFilePath = Path.Combine(appDataPath, _configuration["AgentGovernanceDb:Path"] ?? "agents-governance.db");
            return new SqliteAgentGovernanceRepository($"Data Source={dbFilePath}");
        });

        services.AddRazorComponents().AddInteractiveServerComponents();
        services.AddControllers().AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        });
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        Console.WriteLine($"Environment: {env.EnvironmentName}");
        StaticWebAssetsLoader.UseStaticWebAssets(env, app.ApplicationServices.GetRequiredService<IConfiguration>());
        app.UseSerilogRequestLogging();

        if (!env.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }

        var auditRepository = app.ApplicationServices.GetRequiredService<IAuditRepository>();
        auditRepository.EnsureInitializedAsync(CancellationToken.None).GetAwaiter().GetResult();

        var agentGovernanceRepository = app.ApplicationServices.GetRequiredService<IAgentGovernanceRepository>();
        agentGovernanceRepository.EnsureInitializedAsync(CancellationToken.None).GetAwaiter().GetResult();

        app.UseHttpsRedirection();
        app.UseRouting();
        app.UseMiddleware<RequestIdentityMiddleware>();

        app.UseCors();
        app.UseAuthorization();
        app.UseAntiforgery();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapStaticAssets();
            endpoints.MapControllers();
            endpoints.MapRazorComponents<App>().AddInteractiveServerRenderMode();
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

        throw new InvalidOperationException($"Missing required setting. Provide one of: {string.Join(", ", keys)}");
    }
}
