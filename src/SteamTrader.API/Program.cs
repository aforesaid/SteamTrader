using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace SteamTrader.API
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
                .WriteTo.Console()
                .WriteTo.Seq($"http://{Environment.GetEnvironmentVariable("SEQ")}:5341", Serilog.Events.LogEventLevel.Information)
                .Enrich.WithProperty("APP", "API")
                .CreateLogger();

            try
            {
                Log.Information("Starting");
                
                CreateHostBuilder(args, Log.Logger).Build().Run();
            }
            catch (Exception e)
            {
                Log.Fatal(e, "Crashed");
                Log.CloseAndFlush();
                throw;
            }
        }

        private static IHostBuilder CreateHostBuilder(string[] args, ILogger logger) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                })
                .UseSerilog(logger);
    }
}
