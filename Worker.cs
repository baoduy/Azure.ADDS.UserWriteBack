using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.ADDS.UserWriteBack.Adds;
using Azure.ADDS.UserWriteBack.MsGraph;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// ReSharper disable TemplateIsNotCompileTimeConstantProblem

namespace Azure.ADDS.UserWriteBack
{
    public class Worker : BackgroundService
    {
        private static DateTime? _lastRun = null;
        private readonly ILogger<Worker> _logger;
        private readonly GraphService _graph;
        private readonly AdService _adService;

        public Worker(ILogger<Worker> logger, GraphService graph, AdService adService)
        {
            _logger = logger;
            _graph = graph;
            _adService = adService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Checking the last synced timing");
                if (_lastRun == null || (DateTime.Now - _lastRun)?.TotalHours >= 1)
                {
                    _lastRun = DateTime.Now;
                    _logger.LogInformation("Start Azure AD and AD DS Users Sync up");

                    try
                    {
                        var users = await _graph.GetUsersAsync(stoppingToken);
                        _adService.Upserts(users);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex.Message, ex);
                    }
                }

                _logger.LogInformation("Wait for next 15 minutes");
                await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
            }
        }
    }
}