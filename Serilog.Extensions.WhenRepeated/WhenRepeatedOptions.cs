using Serilog.Events;
using System;

namespace Serilog.Extensions.WhenRepeated
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Blocker Code Smell", "S3427:Method overloads with default parameter values should not overlap ", Justification = "<Pending>")]
    public class WhenRepeatedOptions
    {
        public delegate LogEvent OnRepeatCallback(LogEvent current, LogEvent? previous);
        public delegate bool Comparer(LogEvent current, LogEvent? previous);
        public delegate TimeSpan TimeoutGetter(LogEvent current, LogEvent? previous);
        public delegate OnFirstStrategy OnFirstStrategyGetter(LogEvent current, LogEvent? previous);

        /// <summary>
        /// Initializes an instance of the <see cref="WhenRepeatedOptions"/> using the given constructor call including its argument values.
        /// </summary>
        /// <param name="onRepeat">The function that provides new log event object as substitute of duplicated log event object.</param>
        /// <param name="compare">The function that provides statement of that how to compare current log with previous log event.</param>
        /// <param name="timeout">Duration after which message if duplicated will be logged.</param>
        /// <param name="firstStrategy">Strategy that determines how to handle first (not duplicated) log event.</param>
        public WhenRepeatedOptions(
            OnRepeatCallback? onRepeat = null,
            Comparer? compare = null,
            TimeSpan? timeout = null,
            OnFirstStrategy? firstStrategy = null)
        {
            OnRepeat = onRepeat;
            Compare = compare ?? Compare;
            if (timeout != null)
            {
                Timeout = (_, __) => (TimeSpan)timeout;
            }

            if (firstStrategy != null)
            {
                FirstStrategy = (_, __) => (OnFirstStrategy)firstStrategy;
            }
        }

        /// <summary>
        /// Initializes an instance of the <see cref="WhenRepeatedOptions"/> using the given constructor call including its argument values.
        /// </summary>
        /// <param name="onRepeat">The function that provides new log event object as substitute of duplicated log event object.</param>
        /// <param name="timeout">The function that provides the duration after which message if duplicated will be logged.</param>
        public WhenRepeatedOptions(OnRepeatCallback? onRepeat = null)
        {
            OnRepeat = onRepeat;
        }

        /// <summary>
        /// Initializes an instance of the <see cref="WhenRepeatedOptions"/> using the given constructor call including its argument values.
        /// </summary>
        /// <param name="onRepeat">The function that provides new log event object as substitute of duplicated log event object.</param>
        /// <param name="timeout">The function that provides the duration after which message if duplicated will be logged.</param>
        public WhenRepeatedOptions(
            OnRepeatCallback? onRepeat = null,
            TimeoutGetter? timeout = null)
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
            OnRepeatCallback? onRepeat = null,
            Comparer? compare = null,
            TimeoutGetter? timeout = null)
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
            OnRepeatCallback? onRepeat = null,
            Comparer? compare = null,
            TimeoutGetter? timeout = null,
            OnFirstStrategyGetter? firstStrategy = null)
        {
            OnRepeat = onRepeat;
            Compare = compare ?? Compare;
            Timeout = timeout ?? Timeout;
            FirstStrategy = firstStrategy ?? FirstStrategy;
        }

        internal OnRepeatCallback? OnRepeat { get; private set; }
        internal Comparer Compare { get; private set; } = (current, previous) => current.RenderMessage() == previous?.RenderMessage() && current?.Level == previous?.Level;
        internal TimeoutGetter Timeout { get; private set; } = (current, previous) => Constants.DefaultTimeout;
        internal OnFirstStrategyGetter FirstStrategy { get; private set; } = (current, previous) => OnFirstStrategy.Default;
    }
}
