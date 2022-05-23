using Serilog.Events;
using System;

namespace Serilog.Extensions.WhenRepeated
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Blocker Code Smell", "S3427:Method overloads with default parameter values should not overlap ", Justification = "<Pending>")]
    public class WhenRepeatedOptions
    {
        /// <summary>
        /// Initializes an instance of the <see cref="WhenRepeatedOptions"/> using the given constructor call including its argument values.
        /// </summary>
        /// <param name="onRepeat">The function that provides new log event object as substitute of duplicated log event object.</param>
        /// <param name="compare">The function that provides statement of that how to compare current log with previous log event.</param>
        /// <param name="timeout">Duration after which message if duplicated will be logged.</param>
        /// <param name="firstStrategy">Strategy that determines how to handle first (not duplicated) log event.</param>
        public WhenRepeatedOptions(
            Func<LogEvent, LogEvent?, LogEvent?>? onRepeat = null,
            Func<LogEvent, LogEvent?, bool>? compare = null,
            TimeSpan? timeout = null,
            OnFirstStrategy? firstStrategy = null)
        {
            OnRepeat = onRepeat;
            Compare = compare ?? Compare;
            if (timeout != null)
            {
                Timeout = (_, _) => (TimeSpan)timeout;
            }

            if (firstStrategy != null)
            {
                FirstStrategy = (_, _) => (OnFirstStrategy)firstStrategy;
            }
        }

        /// <summary>
        /// Initializes an instance of the <see cref="WhenRepeatedOptions"/> using the given constructor call including its argument values.
        /// </summary>
        /// <param name="onRepeat">The function that provides new log event object as substitute of duplicated log event object.</param>
        /// <param name="timeout">The function that provides the duration after which message if duplicated will be logged.</param>
        public WhenRepeatedOptions(Func<LogEvent, LogEvent?, LogEvent?>? onRepeat = null)
        {
            OnRepeat = onRepeat;
        }

        /// <summary>
        /// Initializes an instance of the <see cref="WhenRepeatedOptions"/> using the given constructor call including its argument values.
        /// </summary>
        /// <param name="onRepeat">The function that provides new log event object as substitute of duplicated log event object.</param>
        /// <param name="timeout">The function that provides the duration after which message if duplicated will be logged.</param>
        public WhenRepeatedOptions(
            Func<LogEvent, LogEvent?, LogEvent?>? onRepeat = null,
            Func<LogEvent, LogEvent?, TimeSpan>? timeout = null)
        {
            OnRepeat = onRepeat;
            Timeout = timeout ?? Timeout;
        }

        /// <summary>
        /// Initializes an instance of the <see cref="WhenRepeatedOptions"/> using the given constructor call including its argument values.
        /// </summary>
        /// <param name="onRepeat">The function that provides new log event object as substitute of duplicated log event object.</param>
        /// <param name="compare">The function that provides statement of that how to compare current log with previous log event.</param>
        /// <param name="timeout">The function that provides the duration after which message if duplicated will be logged.</param>
        public WhenRepeatedOptions(
            Func<LogEvent, LogEvent?, LogEvent?>? onRepeat = null,
            Func<LogEvent, LogEvent?, bool>? compare = null,
            Func<LogEvent, LogEvent?, TimeSpan>? timeout = null)
        {
            OnRepeat = onRepeat;
            Compare = compare ?? Compare;
            Timeout = timeout ?? Timeout;
        }

        /// <summary>
        /// Initializes an instance of the <see cref="WhenRepeatedOptions"/> using the given constructor call including its argument values.
        /// </summary>
        /// <param name="onRepeat">The function that provides new log event object as substitute of duplicated log event object.</param>
        /// <param name="compare">The function that provides statement of that how to compare current log with previous log event.</param>
        /// <param name="timeout">The function that provides the duration after which message if duplicated will be logged.</param>
        /// <param name="firstStrategy">The function that provides the strategy that determines how to handle first (not duplicated) log event.</param>
        public WhenRepeatedOptions(
            Func<LogEvent, LogEvent?, LogEvent?>? onRepeat = null,
            Func<LogEvent, LogEvent?, bool>? compare = null,
            Func<LogEvent, LogEvent?, TimeSpan>? timeout = null,
            Func<LogEvent, LogEvent?, OnFirstStrategy>? firstStrategy = null)
        {
            OnRepeat = onRepeat;
            Compare = compare ?? Compare;
            Timeout = timeout ?? Timeout;
            FirstStrategy = firstStrategy ?? FirstStrategy;
        }

        internal Func<LogEvent, LogEvent?, LogEvent?>? OnRepeat { get; private set; }
        internal Func<LogEvent, LogEvent?, bool> Compare { get; private set; } = (current, previous) => current.RenderMessage() == previous?.RenderMessage() && current?.Level == previous?.Level;
        internal Func<LogEvent, LogEvent?, TimeSpan> Timeout { get; private set; } = (current, previous) => Constants.DefaultTimeout;
        internal Func<LogEvent, LogEvent?, OnFirstStrategy> FirstStrategy { get; private set; } = (current, previous) => OnFirstStrategy.Default;
    }
}
