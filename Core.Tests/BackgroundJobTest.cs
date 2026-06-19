using Core.BackgroundJobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Core.Tests
{
    public class BackgroundJobTest
    {
        [Fact]
        public void JobExecutionContext_WithoutParameters_InitializesEmptyDictionary()
        {
            var scheduledAt = new DateTimeOffset(2030, 1, 1, 12, 0, 0, TimeSpan.Zero);

            var context = new JobExecutionContext
            {
                JobId = "job-123",
                ScheduledAt = scheduledAt
            };

            Assert.Equal("job-123", context.JobId);
            Assert.Equal(scheduledAt, context.ScheduledAt);
            Assert.NotNull(context.Parameters);
            Assert.Empty(context.Parameters);
        }
    }

    public class BackgroundJobExecutorTest
    {
        [Fact]
        public async Task ExecuteAsync_WithRegisteredJob_ResolvesAndExecutesJob()
        {
            var job = new RecordingBackgroundJob();
            var scopedProvider = new DictionaryServiceProvider(new Dictionary<Type, object>
            {
                [typeof(RecordingBackgroundJob)] = job
            });
            var services = new RootServiceProvider(scopedProvider);
            var logger = new TestLogger<BackgroundJobExecutor>();
            var executor = new BackgroundJobExecutor(services, new IBackgroundJob[] { job }, logger);
            var parameters = new Dictionary<string, string>
            {
                ["region"] = "us-east"
            };
            var before = DateTimeOffset.UtcNow;

            await executor.ExecuteAsync(RecordingBackgroundJob.JobKey, parameters);

            var after = DateTimeOffset.UtcNow;
            var context = Assert.IsType<JobExecutionContext>(job.LastContext);
            Assert.Equal(1, job.ExecuteCount);
            Assert.True(Guid.TryParseExact(context.JobId, "N", out _));
            Assert.InRange(context.ScheduledAt, before, after);
            Assert.Same(parameters, context.Parameters);
            Assert.Equal(CancellationToken.None, job.LastCancellationToken);
            Assert.Collection(
                logger.Entries,
                entry =>
                {
                    Assert.Equal(LogLevel.Information, entry.Level);
                    Assert.Contains("Starting background job", entry.Message);
                    Assert.Contains(RecordingBackgroundJob.JobKey, entry.Message);
                },
                entry =>
                {
                    Assert.Equal(LogLevel.Information, entry.Level);
                    Assert.Contains("Finished background job", entry.Message);
                    Assert.Contains(RecordingBackgroundJob.JobKey, entry.Message);
                });
        }

        [Fact]
        public async Task ExecuteAsync_WithoutParameters_PassesEmptyDictionaryToJob()
        {
            var job = new RecordingBackgroundJob();
            var scopedProvider = new DictionaryServiceProvider(new Dictionary<Type, object>
            {
                [typeof(RecordingBackgroundJob)] = job
            });
            var services = new RootServiceProvider(scopedProvider);
            var logger = new TestLogger<BackgroundJobExecutor>();
            var executor = new BackgroundJobExecutor(services, new IBackgroundJob[] { job }, logger);

            await executor.ExecuteAsync(RecordingBackgroundJob.JobKey);

            var context = Assert.IsType<JobExecutionContext>(job.LastContext);
            Assert.NotNull(context.Parameters);
            Assert.Empty(context.Parameters);
        }

        [Fact]
        public async Task ExecuteAsync_WithUnknownJobKey_ThrowsInvalidOperationException()
        {
            var job = new RecordingBackgroundJob();
            var scopedProvider = new DictionaryServiceProvider(new Dictionary<Type, object>
            {
                [typeof(RecordingBackgroundJob)] = job
            });
            var services = new RootServiceProvider(scopedProvider);
            var logger = new TestLogger<BackgroundJobExecutor>();
            var executor = new BackgroundJobExecutor(services, new IBackgroundJob[] { job }, logger);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => executor.ExecuteAsync("missing-job"));

            Assert.Equal("Unknown job key: missing-job", exception.Message);
            Assert.Empty(logger.Entries);
        }

        private sealed class RecordingBackgroundJob : IBackgroundJob
        {
            public const string JobKey = "recording-job";

            public string Key => JobKey;

            public int ExecuteCount { get; private set; }

            public JobExecutionContext? LastContext { get; private set; }

            public CancellationToken LastCancellationToken { get; private set; }

            public Task ExecuteAsync(JobExecutionContext context, CancellationToken cancellationToken)
            {
                ExecuteCount++;
                LastContext = context;
                LastCancellationToken = cancellationToken;
                return Task.CompletedTask;
            }
        }

        private sealed class RootServiceProvider : IServiceProvider
        {
            private readonly IServiceProvider _scopedServiceProvider;

            public RootServiceProvider(IServiceProvider scopedServiceProvider)
            {
                _scopedServiceProvider = scopedServiceProvider;
            }

            public object? GetService(Type serviceType)
            {
                if (serviceType == typeof(IServiceScopeFactory))
                {
                    return new ScopeFactory(_scopedServiceProvider);
                }

                return null;
            }
        }

        private sealed class ScopeFactory : IServiceScopeFactory
        {
            private readonly IServiceProvider _scopedServiceProvider;

            public ScopeFactory(IServiceProvider scopedServiceProvider)
            {
                _scopedServiceProvider = scopedServiceProvider;
            }

            public IServiceScope CreateScope() => new Scope(_scopedServiceProvider);
        }

        private sealed class Scope : IServiceScope
        {
            public Scope(IServiceProvider serviceProvider)
            {
                ServiceProvider = serviceProvider;
            }

            public IServiceProvider ServiceProvider { get; }

            public void Dispose()
            {
            }
        }

        private sealed class DictionaryServiceProvider : IServiceProvider
        {
            private readonly IReadOnlyDictionary<Type, object> _services;

            public DictionaryServiceProvider(IReadOnlyDictionary<Type, object> services)
            {
                _services = services;
            }

            public object? GetService(Type serviceType)
            {
                return _services.TryGetValue(serviceType, out var service)
                    ? service
                    : null;
            }
        }

        private sealed class TestLogger<T> : ILogger<T>
        {
            public List<LogEntry> Entries { get; } = new();

            public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                Entries.Add(new LogEntry(logLevel, formatter(state, exception)));
            }

            public sealed record LogEntry(LogLevel Level, string Message);

            private sealed class NullScope : IDisposable
            {
                public static NullScope Instance { get; } = new();

                public void Dispose()
                {
                }
            }
        }
    }
}
