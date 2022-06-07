using FluentAssertions;
using Moq;
using Serilog.Core;
using Serilog.Events;
using Serilog.Parsing;
using Serilog.Sinks.InMemory;
using System;
using System.Linq;
using System.Threading;
using Xunit;

namespace Serilog.Extensions.WhenRepeated.Tests
{
    public class InMemoryTests
    {
        // https://github.com/serilog-contrib/SerilogSinksInMemory/issues/12
        private readonly InMemorySink sink = new InMemorySink();
        private const string DefaultMessage = nameof(DefaultMessage);
        private const string RepeatedMessage = nameof(RepeatedMessage);
        private static readonly MessageTemplate RepeatedMessageTemplate = new MessageTemplate(new MessageTemplateParser().Parse(RepeatedMessage).Tokens);

        [Fact]
        public void WhenTimeoutIsZero_ThenWriteEveryRepeatedLogAsRepeatedMessage()
        {
            var logger = new LoggerConfiguration()
                .WriteTo
                .WhenRepeated(
                    configureWrappedSink: x => x.Sink(sink),
                    options: new WhenRepeatedOptions(
                        timeout: (_, __) => TimeSpan.Zero,
                        onRepeat: (current, _) =>
                        {
                            return new LogEvent(
                                timestamp: current.Timestamp,
                                level: current.Level,
                                exception: null,
                                messageTemplate: RepeatedMessageTemplate,
                                properties: Array.Empty<LogEventProperty>());
                        })
                    )
                .CreateLogger();

            logger.Information(DefaultMessage);
            logger.Information(DefaultMessage);
            logger.Information(DefaultMessage);

            sink.LogEvents.Should()
                .ContainSingle(x => x.RenderMessage(null) == DefaultMessage);
            sink.LogEvents
                .Count(x => x.RenderMessage() == RepeatedMessage).Should().Be(2);
        }

        [Fact]
        public void WhenTimeoutIsOneSecond_ThenIgnoreRepeatedMessagesLoggedEarlierThanOneSecondAgo()
        {
            var logger = new LoggerConfiguration()
                .WriteTo
                .WhenRepeated(
                    configureWrappedSink: x => x.Sink(sink),
                    options: new WhenRepeatedOptions(
                        timeout: (_, __) => TimeSpan.FromSeconds(1),
                        onRepeat: (current, _) =>
                        {
                            return new LogEvent(
                                timestamp: current.Timestamp,
                                level: current.Level,
                                exception: null,
                                messageTemplate: RepeatedMessageTemplate,
                                properties: Array.Empty<LogEventProperty>());
                        }
                        ))
                .CreateLogger();

            logger.Information(DefaultMessage);
            logger.Information(DefaultMessage);
            Thread.Sleep(TimeSpan.FromSeconds(1));
            logger.Information(DefaultMessage);

            sink.LogEvents.Should().ContainSingle(x => x.RenderMessage(null) == DefaultMessage);
            sink.LogEvents.Should().ContainSingle(x => x.RenderMessage(null) == RepeatedMessage);
        }

        [Fact]
        public void WhenOnRepeatCallbackReturnsNull_ThenLogOnlyFirstMessage()
        {
            var logger = new LoggerConfiguration()
                .WriteTo
                .WhenRepeated(
                    configureWrappedSink: x => x.Sink(sink),
                    options: new WhenRepeatedOptions(
                        timeout: (_, __) => TimeSpan.FromSeconds(1),
                        onRepeat: (current, _) => null))
                .CreateLogger();

            logger.Information(DefaultMessage);
            logger.Information(DefaultMessage);
            logger.Information(DefaultMessage);

            sink.LogEvents.Should().ContainSingle(x => x.RenderMessage(null) == DefaultMessage);
        }

        [Fact]
        public void GivenFirstStrategyIsWhenRepeated_WhenMessageLoggedTwice_ThenDontLogAnyMessage()
        {
            var logger = new LoggerConfiguration()
                .WriteTo
                .WhenRepeated(
                    configureWrappedSink: x => x.Sink(sink),
                    options: new WhenRepeatedOptions(
                        firstStrategy: (_, __) => OnFirstStrategy.WhenRepeated,
                        timeout: (_, __) => TimeSpan.FromSeconds(1),
                        onRepeat: (current, _) => new LogEvent(
                                timestamp: current.Timestamp,
                                level: current.Level,
                                exception: null,
                                messageTemplate: RepeatedMessageTemplate,
                                properties: Array.Empty<LogEventProperty>())))
                .CreateLogger();

            logger.Information(DefaultMessage);
            logger.Information(DefaultMessage);
            sink.LogEvents.Count(x => x.RenderMessage(null) == DefaultMessage)
                .Should().Be(0, $"'{DefaultMessage}' has been logged second time in shorter time than 1 second");

            Thread.Sleep(TimeSpan.FromSeconds(1));
            logger.Information(RepeatedMessage);

            logger.Information(DefaultMessage);
            logger.Information(DefaultMessage);
            sink.LogEvents
                .Count(x => x.RenderMessage(null) == DefaultMessage)
                .Should().Be(0, $"'{DefaultMessage}' has been logged second time in shorter time than 1 second and has been preceded by '{RepeatedMessage}' message");
        }

        [Fact]
        public void GivenFirstStrategyIsWhenRepeated_WhenMessageLoggedSecondTimeAfterTimeout_ThenLogMessage()
        {
            var logger = new LoggerConfiguration()
                .WriteTo
                .WhenRepeated(
                    configureWrappedSink: x => x.Sink(sink),
                    options: new WhenRepeatedOptions(
                        firstStrategy: (_, __) => OnFirstStrategy.WhenRepeated,
                        timeout: (_, __) => TimeSpan.FromSeconds(1),
                        onRepeat: (current, _) => new LogEvent(
                                timestamp: current.Timestamp,
                                level: current.Level,
                                exception: null,
                                messageTemplate: RepeatedMessageTemplate,
                                properties: Array.Empty<LogEventProperty>())))
                .CreateLogger();

            logger.Information(DefaultMessage);
            Thread.Sleep(TimeSpan.FromSeconds(1));
            logger.Information(DefaultMessage);
            sink.LogEvents.Should()
                .ContainSingle(x => x.RenderMessage(null) == DefaultMessage);
        }

        [Fact]
        public void WhenFirstStrategyIsAsRepeated_ThenInvokeOnRepeatEvenOnFirstLog()
        {
            var logger = new LoggerConfiguration()
                .WriteTo
                .WhenRepeated(
                    configureWrappedSink: x => x.Sink(sink),
                    options: new WhenRepeatedOptions(
                        firstStrategy: (_, __) => OnFirstStrategy.AsRepeated,
                        timeout: (_, __) => TimeSpan.FromSeconds(1),
                        onRepeat: (current, _) =>
                        {
                            return new LogEvent(
                                timestamp: current.Timestamp,
                                level: current.Level,
                                exception: null,
                                messageTemplate: RepeatedMessageTemplate,
                                properties: Array.Empty<LogEventProperty>());
                        }))
                .CreateLogger();

            logger.Information(DefaultMessage);
            sink.LogEvents.Should()
                .ContainSingle(x => x.RenderMessage(null) == RepeatedMessage);
        }

        [Fact]
        public void WhenFirstStrategyIsIgnore_ThenLogSecondMessage()
        {
            var logger = new LoggerConfiguration()
                .WriteTo
                .WhenRepeated(
                    configureWrappedSink: x => x.Sink(sink),
                    options: new WhenRepeatedOptions(
                        firstStrategy: (_, __) => OnFirstStrategy.Ignore,
                        timeout: (_, __) => TimeSpan.Zero))
                .CreateLogger();

            logger.Information(DefaultMessage);
            logger.Information(DefaultMessage);
            sink.LogEvents.Should()
                .ContainSingle(x => x.RenderMessage(null) == DefaultMessage);
        }

        [Fact]
        public void WhenEncrichWithRepeatedMessagesCountEnabled_ThenRepeatCountAvailableInTemplate()
        {
            var logger = new LoggerConfiguration()
                .Enrich.WithRepeatedMessagesCount("repeatCount")
                .WriteTo
                .WhenRepeated(
                    configureWrappedSink: x => x.Sink(sink),
                    options: new WhenRepeatedOptions(
                        firstStrategy: (_, __) => OnFirstStrategy.Default,
                        timeout: (_, __) => TimeSpan.Zero))
                .CreateLogger();

            logger.Information($"{RepeatedMessage} {{repeatCount}}");
            sink.LogEvents.Select(x => x.RenderMessage()).Last().Should().Be($"{RepeatedMessage} 0");

            logger.Information($"{RepeatedMessage} {{repeatCount}}");
            sink.LogEvents.Select(x => x.RenderMessage()).Last().Should().Be($"{RepeatedMessage} 1");

            logger.Information($"{RepeatedMessage} {{repeatCount}}");
            sink.LogEvents.Select(x => x.RenderMessage()).Last().Should().Be($"{RepeatedMessage} 2");

            logger.Information($"{DefaultMessage} {{repeatCount}}");
            sink.LogEvents.Select(x => x.RenderMessage()).Last().Should().Be($"{DefaultMessage} 0");
        }

        [Fact]
        public void WhenRepeatedMessagesCountEnricherPropertyNameIsNullOrWhitespace_ThenThrowException()
        {
            var act = new Action(() =>
            {
                new LoggerConfiguration()
                    .Enrich.WithRepeatedMessagesCount(" ")
                    .WriteTo
                    .WhenRepeated(
                        configureWrappedSink: x => x.Sink(sink),
                        options: new WhenRepeatedOptions(
                            firstStrategy: (_, __) => OnFirstStrategy.Default,
                            timeout: (_, __) => TimeSpan.Zero))
                    .CreateLogger();
            });
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void WhenRepeatedOptionsCtorCalled_ThenRelatedFieldsInitialized()
        {
            var options = new WhenRepeatedOptions(
                onRepeat: (logEvent, _) => logEvent,
                compare: (_, __) => true,
                timeout: TimeSpan.FromSeconds(6),
                firstStrategy: OnFirstStrategy.AsRepeated
                );
            options.OnRepeat.Should().NotBeNull();
            options.Compare.Invoke(It.IsAny<LogEvent>(), It.IsAny<LogEvent>()).Should().BeTrue();
            options.Timeout.Invoke(It.IsAny<LogEvent>(), It.IsAny<LogEvent>()).Should().Be(TimeSpan.FromSeconds(6));
            options.FirstStrategy.Invoke(It.IsAny<LogEvent>(), It.IsAny<LogEvent>()).Should().Be(OnFirstStrategy.AsRepeated);

            options = new WhenRepeatedOptions(
                onRepeat: (logEvent, _) => logEvent,
                timeout: (_, __) => TimeSpan.FromSeconds(6));
            options.OnRepeat.Should().NotBeNull();
            options.Timeout.Invoke(It.IsAny<LogEvent>(), It.IsAny<LogEvent>()).Should().Be(TimeSpan.FromSeconds(6));
            options.FirstStrategy.Invoke(It.IsAny<LogEvent>(), It.IsAny<LogEvent>()).Should().Be(OnFirstStrategy.Default);

            options = new WhenRepeatedOptions(
                onRepeat: (logEvent, _) => logEvent,
                compare: (_, __) => true,
                timeout: (_, __) => TimeSpan.FromSeconds(6));
            options.OnRepeat.Should().NotBeNull();
            options.Compare.Invoke(It.IsAny<LogEvent>(), It.IsAny<LogEvent>()).Should().BeTrue();
            options.Timeout.Invoke(It.IsAny<LogEvent>(), It.IsAny<LogEvent>()).Should().Be(TimeSpan.FromSeconds(6));
            options.FirstStrategy.Invoke(It.IsAny<LogEvent>(), It.IsAny<LogEvent>()).Should().Be(OnFirstStrategy.Default);

            options = new WhenRepeatedOptions(
                onRepeat: (logEvent, _) => logEvent,
                compare: (_, __) => true,
                timeout: (_, __) => TimeSpan.FromSeconds(value: 6),
                firstStrategy: (_, __) => OnFirstStrategy.AsRepeated);
            options.OnRepeat.Should().NotBeNull();
            options.Compare.Invoke(It.IsAny<LogEvent>(), It.IsAny<LogEvent>()).Should().BeTrue();
            options.Timeout.Invoke(It.IsAny<LogEvent>(), It.IsAny<LogEvent>()).Should().Be(TimeSpan.FromSeconds(6));
            options.FirstStrategy.Invoke(It.IsAny<LogEvent>(), It.IsAny<LogEvent>()).Should().Be(OnFirstStrategy.AsRepeated);
        }

        [Fact]
        public void WhenLoggerHasBeenDisposed_ThenSinkDisposeMethodHasBeenCalled()
        {
            var wrappedSink = new Mock<IDisposable>();
            wrappedSink.Setup(x => x.Dispose());
            var sink = new WhenRepeatedEventSink(wrappedSink.As<ILogEventSink>().Object, It.IsAny<WhenRepeatedOptions>());
            sink.Dispose();
            wrappedSink.Verify(mock => mock.Dispose(), Times.Once());
        }

        [Fact]
        public void WhenDefaultTimeoutNotOverriden_ThenDefaultTimeoutIs10Seconds()
        {
            var options = new WhenRepeatedOptions((logEvent, _) => logEvent);
            options.Timeout.Invoke(It.IsAny<LogEvent>(), It.IsAny<LogEvent>()).Should().Be(TimeSpan.FromSeconds(10));
        }
    }
}