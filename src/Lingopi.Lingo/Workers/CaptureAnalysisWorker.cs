using Lingopi.Lingo.Application.Interfaces;
using Minimals.Operations;

namespace Lingopi.Lingo.Workers;

public sealed class CaptureAnalysisWorker(IServiceScopeFactory serviceScopeFactory,
    ILogger<CaptureAnalysisWorker> logger) : BackgroundService
{
    private const int PollingIntervalMilliseconds = 500;
    private bool _noOperationLogged;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var processed = await DoAsync(stoppingToken);
                if (processed)
                {
                    continue;
                }

                await Task.Delay(TimeSpan.FromMilliseconds(PollingIntervalMilliseconds), stoppingToken);
            }
            catch (OperationCanceledException exception) when (stoppingToken.IsCancellationRequested)
            {
                logger.LogInformation(exception, "Capture analysis worker is stopping.");
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Capture analysis worker iteration failed unexpectedly.");
                await Task.Delay(TimeSpan.FromMilliseconds(PollingIntervalMilliseconds), stoppingToken);
            }
        }
    }

    private async Task<bool> DoAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var operation = scope.ServiceProvider.GetRequiredService<IOperationService>().ProcessCapture;

        var result = await operation.ExecuteAsync(new(), cancellationToken);

        if (result.Status == OperationStatus.NoOperation)
        {
            if (!_noOperationLogged)
            {
                logger.LogInformation("Capture analysis operation completed with status {Status}.", result.Status);
                _noOperationLogged = true;
            }

            return false;
        }

        _noOperationLogged = false;
        if (result.Succeeded)
        {
            logger.LogInformation("Capture analysis operation completed with status {Status}.", result.Status);
        }
        else
        {
            logger.LogError("Capture analysis operation failed with status {Status}: {Messages}.", result.Status,
                string.Join("; ", result.Error?.Messages ?? []));
        }

        return true;
    }
}
