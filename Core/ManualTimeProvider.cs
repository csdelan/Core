namespace Core
{
    /// <summary>
    /// A controllable, thread-safe <see cref="TimeProvider"/> whose notion of "now" only moves
    /// when it is advanced manually. Timers created against this provider fire when, and only when,
    /// the clock is advanced across their due time, so a long-running service can be driven through
    /// a simulated timeline deterministically.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is a production-usable reimplementation of the semantics found in the BCL
    /// <c>FakeTimeProvider</c> (from <c>Microsoft.Extensions.TimeProvider.Testing</c>) and Egil
    /// Hansen's <c>ManualTimeProvider</c>, written with no third-party dependencies so it can live
    /// in the lowest-level shared library.
    /// </para>
    /// <para>
    /// Intended usage: register <see cref="TimeProvider.System"/> as the single
    /// <see cref="TimeProvider"/> service in production, and a <see cref="ManualTimeProvider"/>
    /// instance in deterministic simulation and test contexts. Consumers depend only on
    /// <see cref="TimeProvider"/> and never on this concrete type.
    /// </para>
    /// <para><b>Timer-firing contract.</b> Timers do not fire on their own and never fire on a
    /// background thread. A scheduled timer fires only while <see cref="Advance(TimeSpan)"/> or
    /// <see cref="SetUtcNow(DateTimeOffset)"/> moves the clock to or past its due time. When a
    /// single advance spans several due times (including the periods of a repeating timer), the
    /// callbacks are invoked synchronously, in chronological order, the correct number of times.
    /// While each callback runs, <see cref="GetUtcNow"/> reports that callback's due time (not the
    /// final target of the advance), and the clock is set to the advance target only after all due
    /// callbacks have run.</para>
    /// </remarks>
    public sealed class ManualTimeProvider : TimeProvider
    {
        /// <summary>
        /// The default initial instant used when no start time is supplied: 2000-01-01T00:00:00Z.
        /// A fixed epoch (rather than <see cref="DateTimeOffset.UtcNow"/>) keeps tests deterministic.
        /// </summary>
        public static readonly DateTimeOffset DefaultEpoch =
            new(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);

        // System.Threading.Timer's maximum supported due/period in whole milliseconds.
        private const uint MaxSupportedTimeout = 0xfffffffe;

        // Guards _now and _timers. Monitor is re-entrant, so a timer callback running on the
        // advancing thread may safely call back into GetUtcNow/Advance/Change/Dispose.
        private readonly object _lock = new();
        private readonly HashSet<ManualTimer> _timers = new();
        private DateTimeOffset _now;
        private TimeZoneInfo _localTimeZone;
        private TimeSpan _autoAdvanceAmount;

        // Prevents AutoAdvance from recursing when a timer callback (fired during an advance)
        // reads the clock on the same thread.
        [ThreadStatic]
        private static bool _suppressAutoAdvance;

        /// <summary>
        /// Initializes a new <see cref="ManualTimeProvider"/> starting at <see cref="DefaultEpoch"/>
        /// with a UTC <see cref="LocalTimeZone"/>.
        /// </summary>
        public ManualTimeProvider()
            : this(DefaultEpoch, TimeZoneInfo.Utc)
        {
        }

        /// <summary>
        /// Initializes a new <see cref="ManualTimeProvider"/> starting at the given instant with a
        /// UTC <see cref="LocalTimeZone"/>.
        /// </summary>
        /// <param name="startUtcNow">The initial instant reported by <see cref="GetUtcNow"/>.</param>
        public ManualTimeProvider(DateTimeOffset startUtcNow)
            : this(startUtcNow, TimeZoneInfo.Utc)
        {
        }

        /// <summary>
        /// Initializes a new <see cref="ManualTimeProvider"/> starting at the given instant and using
        /// the supplied local time zone.
        /// </summary>
        /// <param name="startUtcNow">The initial instant reported by <see cref="GetUtcNow"/>.</param>
        /// <param name="localTimeZone">The time zone reported by <see cref="LocalTimeZone"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="localTimeZone"/> is <see langword="null"/>.</exception>
        public ManualTimeProvider(DateTimeOffset startUtcNow, TimeZoneInfo localTimeZone)
        {
            ArgumentNullException.ThrowIfNull(localTimeZone);
            _now = startUtcNow.ToUniversalTime();
            _localTimeZone = localTimeZone;
        }

        /// <summary>
        /// Gets or sets the time zone reported by <see cref="GetLocalNow"/>. Defaults to
        /// <see cref="TimeZoneInfo.Utc"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException">The value is <see langword="null"/>.</exception>
        public override TimeZoneInfo LocalTimeZone
        {
            get
            {
                lock (_lock)
                {
                    return _localTimeZone;
                }
            }
        }

        /// <summary>
        /// Sets the time zone reported by <see cref="LocalTimeZone"/> and <see cref="GetLocalNow"/>.
        /// </summary>
        /// <param name="localTimeZone">The time zone to use.</param>
        /// <exception cref="ArgumentNullException"><paramref name="localTimeZone"/> is <see langword="null"/>.</exception>
        public void SetLocalTimeZone(TimeZoneInfo localTimeZone)
        {
            ArgumentNullException.ThrowIfNull(localTimeZone);
            lock (_lock)
            {
                _localTimeZone = localTimeZone;
            }
        }

        /// <summary>
        /// Gets or sets the amount the clock automatically advances on each read of the current time.
        /// When greater than <see cref="TimeSpan.Zero"/>, every call to <see cref="GetUtcNow"/> and
        /// <see cref="GetTimestamp"/> returns the current instant and then advances the clock by this
        /// amount (which may itself fire timers). Defaults to <see cref="TimeSpan.Zero"/> (disabled).
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">The value is negative.</exception>
        public TimeSpan AutoAdvanceAmount
        {
            get
            {
                lock (_lock)
                {
                    return _autoAdvanceAmount;
                }
            }
            set
            {
                if (value < TimeSpan.Zero)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value,
                        "AutoAdvanceAmount must not be negative.");
                }

                lock (_lock)
                {
                    _autoAdvanceAmount = value;
                }
            }
        }

        /// <summary>
        /// Gets the high-resolution timestamp frequency. For a manual clock this is
        /// <see cref="TimeSpan.TicksPerSecond"/> so that <see cref="TimeProvider.GetElapsedTime(long)"/>
        /// is consistent, tick-for-tick, with <see cref="GetUtcNow"/>.
        /// </summary>
        public override long TimestampFrequency => TimeSpan.TicksPerSecond;

        /// <summary>
        /// Returns the current manual instant in UTC. If <see cref="AutoAdvanceAmount"/> is greater
        /// than zero, the clock is advanced by that amount after the returned value is captured.
        /// </summary>
        public override DateTimeOffset GetUtcNow()
        {
            DateTimeOffset captured;
            TimeSpan step;
            lock (_lock)
            {
                captured = _now;
                step = _suppressAutoAdvance ? TimeSpan.Zero : _autoAdvanceAmount;
            }

            if (step > TimeSpan.Zero)
            {
                AutoAdvanceBy(step, captured);
            }

            return captured;
        }

        /// <summary>
        /// Returns a high-resolution timestamp derived from the manual clock. Combined with
        /// <see cref="TimestampFrequency"/>, this makes the <c>Stopwatch</c>/<c>GetElapsedTime</c>
        /// path track the manual clock rather than wall-clock time. If <see cref="AutoAdvanceAmount"/>
        /// is greater than zero, the clock is advanced after the value is captured.
        /// </summary>
        public override long GetTimestamp()
        {
            long captured;
            DateTimeOffset capturedNow;
            TimeSpan step;
            lock (_lock)
            {
                capturedNow = _now;
                captured = _now.UtcTicks;
                step = _suppressAutoAdvance ? TimeSpan.Zero : _autoAdvanceAmount;
            }

            if (step > TimeSpan.Zero)
            {
                AutoAdvanceBy(step, capturedNow);
            }

            return captured;
        }

        /// <summary>
        /// Moves the clock forward by <paramref name="delta"/>, firing any timers whose due time is
        /// crossed (see the timer-firing contract on the type). The clock may only move forward.
        /// </summary>
        /// <param name="delta">A non-negative amount of time to advance.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="delta"/> is negative.</exception>
        public void Advance(TimeSpan delta)
        {
            if (delta < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(delta), delta,
                    "The clock can only move forward; delta must not be negative.");
            }

            DateTimeOffset target;
            lock (_lock)
            {
                target = _now + delta;
            }

            SetUtcNow(target);
        }

        /// <summary>
        /// Sets the clock to <paramref name="value"/>, firing any timers whose due time is crossed
        /// (see the timer-firing contract on the type). The clock may only move forward, so
        /// <paramref name="value"/> must be greater than or equal to the current instant.
        /// </summary>
        /// <param name="value">The instant to set the clock to.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is earlier than the current instant.</exception>
        public void SetUtcNow(DateTimeOffset value)
        {
            value = value.ToUniversalTime();

            // Fire due timers one at a time, recomputing the earliest each pass so callbacks always
            // run in chronological order even if they reschedule timers or create new ones. State is
            // mutated under the lock; the callback itself is invoked outside the lock so user code
            // never blocks unrelated reads (and cannot deadlock on the advancing thread's reentry).
            while (true)
            {
                ManualTimer? toFire = null;
                lock (_lock)
                {
                    if (value < _now)
                    {
                        throw new ArgumentOutOfRangeException(nameof(value), value,
                            "The clock can only move forward; value must not be earlier than the current instant.");
                    }

                    DateTimeOffset earliest = DateTimeOffset.MaxValue;
                    foreach (ManualTimer timer in _timers)
                    {
                        if (timer.Wakeup is DateTimeOffset wakeup && wakeup <= value && wakeup < earliest)
                        {
                            earliest = wakeup;
                            toFire = timer;
                        }
                    }

                    if (toFire is null)
                    {
                        _now = value;
                        return;
                    }

                    _now = earliest;
                    toFire.RescheduleAfterFire();
                }

                toFire.Invoke();
            }
        }

        /// <summary>
        /// Creates a timer bound to this provider. The timer fires according to the timer-firing
        /// contract documented on <see cref="ManualTimeProvider"/>: only while the clock is advanced
        /// across its due time.
        /// </summary>
        /// <param name="callback">The callback invoked when the timer fires.</param>
        /// <param name="state">An optional object passed to the callback.</param>
        /// <param name="dueTime">The delay before the first firing, or <see cref="Timeout.InfiniteTimeSpan"/> to start disabled.</param>
        /// <param name="period">The interval between firings, or <see cref="Timeout.InfiniteTimeSpan"/>/<see cref="TimeSpan.Zero"/> for a one-shot timer.</param>
        /// <returns>An <see cref="ITimer"/> that fires on this manual clock.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="callback"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="dueTime"/> or <paramref name="period"/> is out of range.</exception>
        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            ArgumentNullException.ThrowIfNull(callback);
            ValidateTimerArgs(dueTime, period);

            var timer = new ManualTimer(callback, state, this);
            timer.Change(dueTime, period);
            return timer;
        }

        private void AutoAdvanceBy(TimeSpan step, DateTimeOffset from)
        {
            bool previous = _suppressAutoAdvance;
            _suppressAutoAdvance = true;
            try
            {
                DateTimeOffset target = from + step;

                // Another thread may already have advanced past the target; only move forward.
                lock (_lock)
                {
                    if (target <= _now)
                    {
                        return;
                    }
                }

                SetUtcNow(target);
            }
            finally
            {
                _suppressAutoAdvance = previous;
            }
        }

        private static void ValidateTimerArgs(TimeSpan dueTime, TimeSpan period)
        {
            long dueMs = (long)dueTime.TotalMilliseconds;
            if (dueMs < -1 || dueMs > MaxSupportedTimeout)
            {
                throw new ArgumentOutOfRangeException(nameof(dueTime), dueTime,
                    "dueTime must be Timeout.InfiniteTimeSpan or within the supported timer range.");
            }

            long periodMs = (long)period.TotalMilliseconds;
            if (periodMs < -1 || periodMs > MaxSupportedTimeout)
            {
                throw new ArgumentOutOfRangeException(nameof(period), period,
                    "period must be Timeout.InfiniteTimeSpan or within the supported timer range.");
            }
        }

        /// <summary>
        /// A timer whose firing is driven entirely by the owning <see cref="ManualTimeProvider"/>.
        /// All mutable state is accessed under the provider's lock.
        /// </summary>
        private sealed class ManualTimer : ITimer
        {
            private readonly ManualTimeProvider _provider;
            private TimerCallback? _callback;
            private readonly object? _state;
            private TimeSpan _period;
            private bool _disposed;

            /// <summary>
            /// The absolute instant at which this timer is next due, or <see langword="null"/> when
            /// it is disabled. Read and written only under <see cref="_provider"/>'s lock.
            /// </summary>
            internal DateTimeOffset? Wakeup;

            internal ManualTimer(TimerCallback callback, object? state, ManualTimeProvider provider)
            {
                _callback = callback;
                _state = state;
                _provider = provider;
            }

            public bool Change(TimeSpan dueTime, TimeSpan period)
            {
                ValidateTimerArgs(dueTime, period);

                lock (_provider._lock)
                {
                    if (_disposed)
                    {
                        return false;
                    }

                    _period = period;
                    if (dueTime == Timeout.InfiniteTimeSpan)
                    {
                        Wakeup = null;
                        _provider._timers.Remove(this);
                    }
                    else
                    {
                        Wakeup = _provider._now + dueTime;
                        _provider._timers.Add(this);
                    }
                }

                return true;
            }

            /// <summary>
            /// Called under the provider lock immediately before the callback is invoked. Reschedules
            /// a repeating timer to its next period based on the firing instant, or disables and
            /// removes a one-shot timer.
            /// </summary>
            internal void RescheduleAfterFire()
            {
                if (_period > TimeSpan.Zero && _period != Timeout.InfiniteTimeSpan && Wakeup is DateTimeOffset fired)
                {
                    Wakeup = fired + _period;
                }
                else
                {
                    Wakeup = null;
                    _provider._timers.Remove(this);
                }
            }

            /// <summary>Invokes the callback. Called outside the provider lock.</summary>
            internal void Invoke() => _callback?.Invoke(_state);

            public void Dispose()
            {
                lock (_provider._lock)
                {
                    _disposed = true;
                    _callback = null;
                    Wakeup = null;
                    _provider._timers.Remove(this);
                }
            }

            public ValueTask DisposeAsync()
            {
                Dispose();
                return ValueTask.CompletedTask;
            }
        }
    }
}
