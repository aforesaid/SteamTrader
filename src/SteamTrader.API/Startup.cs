using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SteamTrader.API.Extensions;
using SteamTrader.Core.Configuration;
using SteamTrader.Infrastructure.Data;

namespace SteamTrader.API
{
    public class Startup
    {
        private IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddBusinessLogicLayerServicesExtensions();

            services.Configure<Settings>(Configuration.GetSection(nameof(Settings)));
            services.AddDbContext<SteamTraderDbContext>(x =>
            {
                x.UseNpgsql(Configuration["POSTGRESQL"], npgsql => 
                    npgsql.MigrationsAssembly("SteamTrader.Infrastructure.Data"));
            });
        }
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env,
            SteamTraderDbContext dbContext)
        {
            dbContext.Database.Migrate();
            
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            
            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDefaultControllerRoute();
            });
        }
    }
}
