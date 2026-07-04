namespace Core
{
    /// <summary>
    /// Outcome of a single <see cref="IBackgroundJob"/> run. Captures success/failure, timing, and
    /// any error message — the "what happened" companion to <see cref="BackgroundJobExecutor"/>.
    /// </summary>
    public sealed record JobResult
    {
        public required string JobKey { get; init; }
        public required string JobId { get; init; }
        public required bool Succeeded { get; init; }
        public required DateTimeOffset StartedAt { get; init; }
        public required TimeSpan Duration { get; init; }
        public string? Error { get; init; }

        public static JobResult Success(string jobKey, string jobId, DateTimeOffset startedAt, TimeSpan duration) =>
            new() { JobKey = jobKey, JobId = jobId, Succeeded = true, StartedAt = startedAt, Duration = duration };

        public static JobResult Failure(string jobKey, string jobId, DateTimeOffset startedAt, TimeSpan duration, string error) =>
            new() { JobKey = jobKey, JobId = jobId, Succeeded = false, StartedAt = startedAt, Duration = duration, Error = error };
    }
}
