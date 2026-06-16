using System.Collections.Concurrent;

namespace Core.Tests
{
    public class ManualTimeProviderTest
    {
        [Fact]
        public void DefaultConstructor_StartsAtDeterministicEpoch()
        {
            var time = new ManualTimeProvider();

            Assert.Equal(ManualTimeProvider.DefaultEpoch, time.GetUtcNow());
            Assert.Equal(TimeZoneInfo.Utc, time.LocalTimeZone);
        }

        [Fact]
        public void Advance_MovesGetUtcNowForward()
        {
            var start = new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);
            var time = new ManualTimeProvider(start);

            time.Advance(TimeSpan.FromMinutes(5));

            Assert.Equal(start + TimeSpan.FromMinutes(5), time.GetUtcNow());
        }

        [Fact]
        public void SetUtcNow_MovesClockToInstant()
        {
            var time = new ManualTimeProvider();
            var target = ManualTimeProvider.DefaultEpoch + TimeSpan.FromDays(3);

            time.SetUtcNow(target);

            Assert.Equal(target, time.GetUtcNow());
        }

        [Fact]
        public void Advance_NegativeDelta_Throws()
        {
            var time = new ManualTimeProvider();

            Assert.Throws<ArgumentOutOfRangeException>(() => time.Advance(TimeSpan.FromSeconds(-1)));
        }

        [Fact]
        public void SetUtcNow_GoingBackwards_Throws()
        {
            var time = new ManualTimeProvider();
            time.Advance(TimeSpan.FromHours(1));

            Assert.Throws<ArgumentOutOfRangeException>(
                () => time.SetUtcNow(ManualTimeProvider.DefaultEpoch));
        }

        [Fact]
        public void GetElapsedTime_IsConsistentWithGetUtcNow()
        {
            var time = new ManualTimeProvider();

            long start = time.GetTimestamp();
            time.Advance(TimeSpan.FromMilliseconds(1234));
            TimeSpan elapsed = time.GetElapsedTime(start);

            Assert.Equal(TimeSpan.FromMilliseconds(1234), elapsed);
        }

        [Fact]
        public void OneShotTimer_FiresExactlyOnce_WhenAdvanceCrossesDueTime()
        {
            var time = new ManualTimeProvider();
            int count = 0;
            using ITimer timer = time.CreateTimer(_ => count++, null,
                TimeSpan.FromSeconds(10), Timeout.InfiniteTimeSpan);

            time.Advance(TimeSpan.FromSeconds(15));

            Assert.Equal(1, count);

            // Further advances must not fire it again.
            time.Advance(TimeSpan.FromSeconds(100));
            Assert.Equal(1, count);
        }

        [Fact]
        public void OneShotTimer_DoesNotFire_WhenAdvanceStopsShort()
        {
            var time = new ManualTimeProvider();
            int count = 0;
            using ITimer timer = time.CreateTimer(_ => count++, null,
                TimeSpan.FromSeconds(10), Timeout.InfiniteTimeSpan);

            time.Advance(TimeSpan.FromSeconds(9));

            Assert.Equal(0, count);

            // Exactly reaching the due time fires it.
            time.Advance(TimeSpan.FromSeconds(1));
            Assert.Equal(1, count);
        }

        [Fact]
        public void Timer_ObservesDueTime_NotAdvanceTarget_DuringCallback()
        {
            var time = new ManualTimeProvider();
            DateTimeOffset due = ManualTimeProvider.DefaultEpoch + TimeSpan.FromSeconds(10);
            DateTimeOffset observed = default;

            using ITimer timer = time.CreateTimer(_ => observed = time.GetUtcNow(), null,
                TimeSpan.FromSeconds(10), Timeout.InfiniteTimeSpan);

            time.Advance(TimeSpan.FromSeconds(60));

            Assert.Equal(due, observed);
            Assert.Equal(ManualTimeProvider.DefaultEpoch + TimeSpan.FromSeconds(60), time.GetUtcNow());
        }

        [Fact]
        public void RepeatingTimer_FiresCorrectCount_WhenSingleAdvanceSpansSeveralPeriods()
        {
            var time = new ManualTimeProvider();
            var fireTimes = new List<DateTimeOffset>();
            using ITimer timer = time.CreateTimer(_ => fireTimes.Add(time.GetUtcNow()), null,
                TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));

            time.Advance(TimeSpan.FromSeconds(5));

            // Fires at +1, +2, +3, +4, +5.
            Assert.Equal(5, fireTimes.Count);
            for (int i = 0; i < fireTimes.Count; i++)
            {
                Assert.Equal(ManualTimeProvider.DefaultEpoch + TimeSpan.FromSeconds(i + 1), fireTimes[i]);
            }

            // Ordering is chronological.
            var ordered = fireTimes.OrderBy(t => t).ToList();
            Assert.Equal(ordered, fireTimes);
        }

        [Fact]
        public void MultipleTimers_FireInChronologicalOrder_AcrossSingleAdvance()
        {
            var time = new ManualTimeProvider();
            var order = new List<string>();

            using ITimer a = time.CreateTimer(_ => order.Add("a"), null,
                TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(3)); // 3, 6, 9
            using ITimer b = time.CreateTimer(_ => order.Add("b"), null,
                TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5)); // 5, 10

            time.Advance(TimeSpan.FromSeconds(10));

            Assert.Equal(new[] { "a", "b", "a", "a", "b" }, order);
        }

        [Fact]
        public async Task TaskDelay_CompletesOnlyWhenClockAdvancedPastDelay()
        {
            var time = new ManualTimeProvider();
            Task delay = Task.Delay(TimeSpan.FromSeconds(30), time);

            time.Advance(TimeSpan.FromSeconds(29));
            Assert.False(delay.IsCompleted);

            time.Advance(TimeSpan.FromSeconds(1));
            await delay.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.True(delay.IsCompletedSuccessfully);
        }

        [Fact]
        public void DisposedTimer_DoesNotFire()
        {
            var time = new ManualTimeProvider();
            int count = 0;
            ITimer timer = time.CreateTimer(_ => count++, null,
                TimeSpan.FromSeconds(10), Timeout.InfiniteTimeSpan);

            timer.Dispose();
            time.Advance(TimeSpan.FromSeconds(20));

            Assert.Equal(0, count);
        }

        [Fact]
        public void Change_Reschedules_Timer()
        {
            var time = new ManualTimeProvider();
            int count = 0;
            using ITimer timer = time.CreateTimer(_ => count++, null,
                TimeSpan.FromSeconds(10), Timeout.InfiniteTimeSpan);

            timer.Change(TimeSpan.FromSeconds(100), Timeout.InfiniteTimeSpan);
            time.Advance(TimeSpan.FromSeconds(20));
            Assert.Equal(0, count);

            time.Advance(TimeSpan.FromSeconds(90));
            Assert.Equal(1, count);
        }

        [Fact]
        public void AutoAdvance_AdvancesOnEachRead()
        {
            var time = new ManualTimeProvider
            {
                AutoAdvanceAmount = TimeSpan.FromSeconds(1)
            };

            DateTimeOffset first = time.GetUtcNow();
            DateTimeOffset second = time.GetUtcNow();

            Assert.Equal(ManualTimeProvider.DefaultEpoch, first);
            Assert.Equal(ManualTimeProvider.DefaultEpoch + TimeSpan.FromSeconds(1), second);
        }

        [Fact]
        public async Task ConcurrencySmoke_ParallelAdvanceReadsAndRegistration_DoNotDeadlockOrLoseCallbacks()
        {
            var time = new ManualTimeProvider();
            var fired = new ConcurrentBag<int>();
            const int timerCount = 200;
            var timers = new List<ITimer>();

            // Each timer is due at a distinct second within the advance window.
            for (int i = 0; i < timerCount; i++)
            {
                int id = i;
                timers.Add(time.CreateTimer(_ => fired.Add(id), null,
                    TimeSpan.FromSeconds(i + 1), Timeout.InfiniteTimeSpan));
            }

            using var cts = new CancellationTokenSource();

            Task reader = Task.Run(() =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    _ = time.GetUtcNow();
                    _ = time.GetTimestamp();
                }
            });

            Task registrar = Task.Run(() =>
            {
                var extra = new List<ITimer>();
                for (int i = 0; i < 100; i++)
                {
                    extra.Add(time.CreateTimer(_ => { }, null,
                        TimeSpan.FromSeconds(1000), Timeout.InfiniteTimeSpan));
                }

                foreach (ITimer t in extra)
                {
                    t.Dispose();
                }
            });

            // Advance in many small steps from several threads simultaneously.
            Task[] advancers = Enumerable.Range(0, 4).Select(_ => Task.Run(() =>
            {
                for (int i = 0; i < 60; i++)
                {
                    time.Advance(TimeSpan.FromSeconds(5));
                }
            })).ToArray();

            await Task.WhenAll(advancers);
            await registrar;
            cts.Cancel();
            await reader;

            foreach (ITimer t in timers)
            {
                t.Dispose();
            }

            // Every one-shot timer fired exactly once; none lost or duplicated.
            Assert.Equal(timerCount, fired.Count);
            Assert.Equal(Enumerable.Range(0, timerCount), fired.OrderBy(x => x));
        }
    }
}
