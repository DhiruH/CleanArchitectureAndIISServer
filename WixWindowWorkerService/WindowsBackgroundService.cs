using System.Diagnostics;
using System.Runtime.Versioning;

namespace WixWindowWorkerService
{
    [SupportedOSPlatform("windows")]
    public sealed class WindowsBackgroundService(
        JokeService jokeService,
        ILogger<WindowsBackgroundService> logger) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {

                while (!stoppingToken.IsCancellationRequested)
                {
                    //while (!System.Diagnostics.Debugger.IsAttached) Thread.Sleep(100);
                    string joke = jokeService.GetJoke();
                    LogPerformanceMetrics();
                    logger.LogWarning("{Joke}", joke);
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                // When the stopping token is canceled, for example, a call made from services.msc,
                // we shouldn't exit with a non-zero exit code. In other words, this is expected...
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "{Message}", ex.Message);

                // Terminates this process and returns an exit code to the operating system.
                // This is required to avoid the 'BackgroundServiceExceptionBehavior', which
                // performs one of two scenarios:
                // 1. When set to "Ignore": will do nothing at all, errors cause zombie services.
                // 2. When set to "StopHost": will cleanly stop the host, and log errors.
                //
                // In order for the Windows Service Management system to leverage configured
                // recovery options, we need to terminate the process with a non-zero exit code.
                Environment.Exit(1);
            }
        }
        private void LogPerformanceMetrics()
        {
            var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            var memoryCounter = new PerformanceCounter("Memory", "Available MBytes");

            float cpuUsage = cpuCounter.NextValue();
            float availableMemory = memoryCounter.NextValue();

            logger.LogInformation($"CPU Usage: {cpuUsage}%");
            logger.LogInformation($"Available Memory: {availableMemory} MB");
        }
    }
}
