using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Serilog.Extensions.WhenRepeated
{
    internal sealed class WhenRepeatedEventSink : ILogEventSink, IDisposable
    {
        private readonly ILogEventSink wrappedSink;
        private readonly WhenRepeatedOptions options;
        private BigInteger repeatedCounts = 0;
        private LogEvent? previousLogEvent;
        private bool isDisposed;

        public WhenRepeatedEventSink(ILogEventSink wrappedSink, WhenRepeatedOptions options)
        {
            this.wrappedSink = wrappedSink;
            this.options = options;
        }

        ~WhenRepeatedEventSink() => Dispose(false);

        public void Emit(LogEvent logEvent)
        {
            var firstStrategy = options.FirstStrategy.Invoke(logEvent, previousLogEvent);
            var repeatCountPropertyName = logEvent.Properties.TryGetValue(Constants.RepeatedMessagesCountPropertyNameProperty, out var _repeatCountPropertyName)
                ? _repeatCountPropertyName.ToString().Replace("\"", "")
                : string.Empty;
            if (!string.IsNullOrWhiteSpace(repeatCountPropertyName))
            {
                ((Dictionary<string, LogEventPropertyValue>)logEvent.Properties)[repeatCountPropertyName] = new ScalarValue(repeatedCounts);
            }
            if (previousLogEvent != null && options.Compare.Invoke(logEvent, previousLogEvent))
            {
                if (logEvent.Timestamp - previousLogEvent.Timestamp > options.Timeout.Invoke(logEvent, previousLogEvent))
                {
                    repeatedCounts++;
                    if (firstStrategy == OnFirstStrategy.WhenRepeated && repeatedCounts == 1)
                    {
                        wrappedSink.Emit(logEvent);
                        return;
                    }
                    else
                    {
                        wrappedSink.Emit(options?.OnRepeat?.Invoke(logEvent, previousLogEvent) ?? logEvent);
                    }

                    if (!string.IsNullOrWhiteSpace(repeatCountPropertyName))
                    {
                        ((Dictionary<string, LogEventPropertyValue>)logEvent.Properties)[repeatCountPropertyName] = new ScalarValue(repeatedCounts);
                    }
                }

                previousLogEvent = logEvent;
                return;
            }

            repeatedCounts = 0;
            if (!string.IsNullOrWhiteSpace(repeatCountPropertyName))
            {
                ((Dictionary<string, LogEventPropertyValue>)logEvent.Properties)[repeatCountPropertyName] = new ScalarValue(repeatedCounts);
            }

            previousLogEvent = logEvent;
            switch (firstStrategy)
            {
                case OnFirstStrategy.Default:
                    wrappedSink.Emit(logEvent);
                    break;
                case OnFirstStrategy.AsRepeated:
                    wrappedSink.Emit(options.OnRepeat?.Invoke(logEvent, previousLogEvent) ?? logEvent);
                    break;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (isDisposed)
            {
                return;
            }

            if (disposing)
            {
                (wrappedSink as IDisposable)?.Dispose();
            }

            isDisposed = true;
        }
    }
}
