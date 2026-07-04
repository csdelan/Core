using System;
using System.Collections.Generic;
using System.Text;

namespace Core
{
    /// <summary>
    /// Represents a background job that can be executed asynchronously.
    /// </summary>
    public interface IBackgroundJob
    {
        /// <summary>
        /// Gets the unique key that identifies the background job.
        /// </summary>
        string Key { get; }

        /// <summary>
        /// Executes the background job asynchronously.
        /// </summary>
        /// <param name="context">The context for the job execution.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        Task ExecuteAsync(JobExecutionContext context, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Represents the context for executing a background job, including job ID, scheduled time, and parameters.
    /// </summary>
    public sealed class JobExecutionContext
    {
        public required string JobId { get; init; }
        public required DateTimeOffset ScheduledAt { get; init; }
        public Dictionary<string, string> Parameters { get; init; } = new();
    }
}
