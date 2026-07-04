using System.Collections.Concurrent;

namespace Core
{
    /// <summary>
    /// In-memory record of the most recent <see cref="JobResult"/> per job key. Thread-safe singleton
    /// suitable for use by heartbeat or monitoring services to summarise recent worker activity.
    /// </summary>
    public sealed class JobRunRegistry
    {
        private readonly ConcurrentDictionary<string, JobResult> _lastRuns = new();

        public void Record(JobResult result) => _lastRuns[result.JobKey] = result;

        public IReadOnlyDictionary<string, JobResult> Snapshot() =>
            new Dictionary<string, JobResult>(_lastRuns);
    }
}
