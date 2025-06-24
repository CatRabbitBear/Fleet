using Fleet.Blazor.Components;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Fleet.Blazor;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // migrate all your builder.Services.AddXyz(...) calls here
        services.AddCors();
        services.AddControllers();
        // Add services to the container.
        services.AddRazorComponents()
            .AddInteractiveServerComponents();

        // ... razor pages, DI registrations, etc.
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (!env.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }

        app.UseStaticFiles();
        app.UseHttpsRedirection();
        app.UseRouting();

        app.UseCors();
        app.UseAuthorization();
        app.UseAntiforgery();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapGet("/api/status", () => "Fleet Web Server Running!");
            endpoints.MapRazorComponents<App>().AddInteractiveServerRenderMode(); // Moved to endpoints
            // endpoints.MapRazorPages(); etc.
        });
    }
}
