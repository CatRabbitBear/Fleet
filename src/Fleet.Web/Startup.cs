// Sleepr.Web/Startup.cs
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Fleet.Web
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            // migrate all your builder.Services.AddXyz(...) calls here
            services.AddCors();
            services.AddControllers();
            
            // ... razor pages, DI registrations, etc.
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (!env.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseCors();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapGet("/api/status", () => "Sleepr Web Server Running!");
                // endpoints.MapRazorPages(); etc.
            });
        }
    }
}