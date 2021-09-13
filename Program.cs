using System;
using Azure.ADDS.UserWriteBack.Adds;
using Azure.ADDS.UserWriteBack.MsGraph;
using Azure.ADDS.UserWriteBack.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

// ReSharper disable MemberCanBePrivate.Global

namespace Azure.ADDS.UserWriteBack
{
    public static class Program
    {
        public static void Main(string[] args) => CreateHostBuilder(args).Build().Run();

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseWindowsService()
                .UseSerilog((_, configuration) =>
                {
                    configuration.WriteTo.File("Logs/log-.txt", 
                        rollingInterval: RollingInterval.Day,
                        retainedFileTimeLimit: TimeSpan.FromDays(30));
#if DEBUG
                    configuration.MinimumLevel.Debug();
#else
                    configuration.MinimumLevel.Information();
#endif
                })
                .ConfigureAppConfiguration(c =>
                {
                    c.AddJsonFile("appsettings.json", false)
                        .AddEnvironmentVariables();
#if DEBUG
                    c.AddJsonFile("appsettings.Development.json", true);
#endif
                })
                .ConfigureServices((hostContext, services) =>
                {
                    var configuration = hostContext.Configuration;
                    services.Configure<AzAdSyncOptions>(configuration.GetSection(AzAdSyncOptions.Name));
                    
                    services
                        .AddSingleton<GraphService>()
                        .AddSingleton<AdService>()
                        .AddHostedService<Worker>();
                });
    }
}