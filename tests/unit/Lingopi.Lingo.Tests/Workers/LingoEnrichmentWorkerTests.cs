#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using Lingopi.Lingo.Application.Interfaces;
using Lingopi.Lingo.Application.Operations.LingoEnrichment;
using Lingopi.Lingo.Workers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Minimals.Operations;
using NSubstitute;
using Xunit;

namespace Lingopi.Lingo.Tests.Workers;

public class LingoEnrichmentWorkerTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldInvokeEnrichmentOperation()
    {
        var operation = Substitute.For<IOperation<EnrichLingoCommand, string>>();
        var invocation = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        operation.ExecuteAsync(Arg.Any<EnrichLingoCommand>(), Arg.Any<CancellationToken?>())
            .Returns(callInfo =>
            {
                invocation.TrySetResult(true);
                return Task.FromResult(OperationResult<string>.NoOperation(string.Empty));
            });

        var services = new ServiceCollection();
        var operationService = Substitute.For<IOperationService>();
        operationService.EnrichLingo.Returns(operation);
        services.AddScoped(_ => operationService);
        await using var provider = services.BuildServiceProvider();

        var worker = new LingoEnrichmentWorker(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<LingoEnrichmentWorker>.Instance);

        var cancellationToken = TestContext.Current.CancellationToken;
        await worker.StartAsync(cancellationToken);
        await invocation.Task.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
        await worker.StopAsync(cancellationToken);

        await operation.Received(1).ExecuteAsync(
            Arg.Is<EnrichLingoCommand>(command =>
                command.Job == null),
            Arg.Any<CancellationToken?>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenNoOperationIsRepeated_LogsItOnce()
    {
        var operation = Substitute.For<IOperation<EnrichLingoCommand, string>>();
        var invocationCount = 0;
        var repeatedInvocations = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        operation.ExecuteAsync(Arg.Any<EnrichLingoCommand>(), Arg.Any<CancellationToken?>())
            .Returns(_ =>
            {
                if (Interlocked.Increment(ref invocationCount) >= 3)
                {
                    repeatedInvocations.TrySetResult(true);
                }

                return Task.FromResult(OperationResult<string>.NoOperation(string.Empty));
            });

        var services = new ServiceCollection();
        var operationService = Substitute.For<IOperationService>();
        operationService.EnrichLingo.Returns(operation);
        services.AddScoped(_ => operationService);
        await using var provider = services.BuildServiceProvider();
        var logger = new RecordingLogger();
        var worker = new LingoEnrichmentWorker(provider.GetRequiredService<IServiceScopeFactory>(), logger);

        var cancellationToken = TestContext.Current.CancellationToken;
        await worker.StartAsync(cancellationToken);
        await repeatedInvocations.Task.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
        await worker.StopAsync(cancellationToken);

        Assert.Equal(1, logger.NoOperationInformationCount);
    }

    private sealed class RecordingLogger : ILogger<LingoEnrichmentWorker>
    {
        public int NoOperationInformationCount { get; private set; }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Information &&
                formatter(state, exception).Contains("NoOperation", StringComparison.Ordinal))
            {
                NoOperationInformationCount++;
            }
        }
    }

}
