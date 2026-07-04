using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Core
{
    /// <summary>
    /// Executes background jobs by resolving them from the service provider and invoking their execution logic.
    /// </summary>
    public sealed class BackgroundJobExecutor
    {
        private readonly IServiceProvider _services;
        private readonly IReadOnlyDictionary<string, Type> _jobTypes;
        private readonly ILogger<BackgroundJobExecutor> _logger;

        public BackgroundJobExecutor(
            IServiceProvider services,
            IEnumerable<IBackgroundJob> jobs,
            ILogger<BackgroundJobExecutor> logger)
        {
            _services = services;
            _logger = logger;
            _jobTypes = jobs.ToDictionary(x => x.Key, x => x.GetType());
        }

        /// <summary>
        /// Executes a background job identified by the specified job key, passing optional parameters to the job execution context.
        /// </summary>
        /// <param name="jobKey">The key that identifies the background job to execute.</param>
        /// <param name="parameters">Optional parameters to pass to the job execution context.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the specified job key is not recognized.</exception>
        public async Task ExecuteAsync(string jobKey, Dictionary<string, string>? parameters = null)
        {
            if (!_jobTypes.TryGetValue(jobKey, out var jobType))
                throw new InvalidOperationException($"Unknown job key: {jobKey}");

            using var scope = _services.CreateScope();

            var job = (IBackgroundJob)scope.ServiceProvider.GetRequiredService(jobType);

            var context = new JobExecutionContext
            {
                JobId = Guid.NewGuid().ToString("N"),
                ScheduledAt = DateTimeOffset.UtcNow,
                Parameters = parameters ?? new()
            };

            _logger.LogInformation("Starting background job {JobKey}", jobKey);

            await job.ExecuteAsync(context, CancellationToken.None);

            _logger.LogInformation("Finished background job {JobKey}", jobKey);
        }
    }
}
