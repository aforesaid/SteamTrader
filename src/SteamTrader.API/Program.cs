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
            var logger = new LoggerConfiguration()
                .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
                .WriteTo.Console()
                .Enrich.WithProperty("APP", "API");

            var seq = Environment.GetEnvironmentVariable("SEQ");
            if (seq != null)
                logger.WriteTo.Seq($"http://{Environment.GetEnvironmentVariable("SEQ")}:5341",
                    Serilog.Events.LogEventLevel.Information);

            Log.Logger = logger.CreateLogger();


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
