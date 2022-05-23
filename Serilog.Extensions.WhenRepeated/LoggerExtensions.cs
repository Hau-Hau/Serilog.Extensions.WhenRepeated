using Serilog.Configuration;
using Serilog.Events;
using System;

namespace Serilog.Extensions.WhenRepeated
{
    public static class LoggerExtensions
    {
        /// <summary>
        /// Configure the enricher.
        /// </summary>
        /// <param name="propertyName">Configures property name under which will be available repeated count property.</param>
        public static LoggerConfiguration WithRepeatedMessagesCount(this LoggerEnrichmentConfiguration that, string propertyName)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
            {
                throw new ArgumentNullException(nameof(propertyName), "propertyName cannot be null or whitespace");
            }
            return that.With(new RepeatedMessagesCountEnricher(propertyName));
        }

        /// <summary>
        /// Configure the <see cref="WhenRepeatedEventSink"/> sink.
        /// </summary>
        /// <param name="configureWrappedSink">An action that configures sinks to be wrapped in <paramref name="wrapSink"/>.</param>
        /// <param name="options">Options that configures the <see cref="WhenRepeatedEventSink"/> sink.</param>
        /// <param name="restrictedToMinimumLevel">The minimum level for events passed through the sink.</param>
        public static LoggerConfiguration WhenRepeated(this LoggerSinkConfiguration that,
            Action<LoggerSinkConfiguration> configureWrappedSink, WhenRepeatedOptions options,
            LogEventLevel restrictedToMinimumLevel = LogEventLevel.Information)
        {
            return LoggerSinkConfiguration.Wrap(
                loggerSinkConfiguration: that,
                wrapSink: x => new WhenRepeatedEventSink(x, options),
                configureWrappedSink: configureWrappedSink,
                restrictedToMinimumLevel: restrictedToMinimumLevel,
                levelSwitch: null);
        }
    }
}