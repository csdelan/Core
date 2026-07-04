namespace Core
{
    public static class DateTimeOffsetExtensions
    {
        /// <summary>
        /// Returns a new DateTimeOffset with the specified day, preserving the year, month, time, and offset.
        /// </summary>
        public static DateTimeOffset WithDay(this DateTimeOffset date, int day)
        {
            return new DateTimeOffset(date.Year, date.Month, day, date.Hour, date.Minute, date.Second, date.Offset);
        }

        /// <summary>
        /// Returns a new DateTimeOffset with the specified month and day, preserving the year, time, and offset.
        /// </summary>
        public static DateTimeOffset WithDayAndMonth(this DateTimeOffset date, int month, int day)
        {
            return new DateTimeOffset(date.Year, month, day, date.Hour, date.Minute, date.Second, date.Offset);
        }

        /// <summary>
        /// Truncates a DateTimeOffset to the nearest minute by removing seconds and milliseconds.
        /// </summary>
        public static DateTimeOffset TruncateToMinute(this DateTimeOffset dateTime)
        {
            var newDTO = new DateTimeOffset(dateTime.Year, dateTime.Month, dateTime.Day,
                                    dateTime.Hour, dateTime.Minute, 0, dateTime.Offset);
            return newDTO;
        }
    }
}
