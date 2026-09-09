using EnterpriseAgenticRag.Application;

namespace EnterpriseAgenticRag.Ingestion.Worker;

public sealed partial class IngestionWorker(IServiceScopeFactory scopeFactory, ILogger<IngestionWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
                var service = scope.ServiceProvider.GetRequiredService<IngestionService>();
                bool processed = await service.ProcessNextAsync(stoppingToken);
                if (!processed) await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception exception)
            {
                LogPollingFailure(logger, exception);
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    [LoggerMessage(LogLevel.Error, "Ingestion polling failed.")]
    private static partial void LogPollingFailure(ILogger logger, Exception exception);
}
