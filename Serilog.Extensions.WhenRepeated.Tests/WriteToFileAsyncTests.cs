using FluentAssertions;
using Polly;
using Serilog.Events;
using Serilog.Extensions.WhenRepeated.Tests.Utils;
using Serilog.Parsing;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Serilog.Extensions.WhenRepeated.Tests
{
    public class WriteToFileAsyncTests : IDisposable
    {
        private const string DefaultMessage = nameof(DefaultMessage);
        private const string RepeatedMessage = nameof(RepeatedMessage);
        private static readonly MessageTemplate RepeatedMessageTemplate = new MessageTemplate(new MessageTemplateParser().Parse(RepeatedMessage).Tokens);
        public static readonly string LogFilesDirectoryPath = Path.Combine(Path.GetTempPath(), "Serilog.Extensions.WhenRepeated");

        private readonly string logFilePath;
        public WriteToFileAsyncTests()
        {
            logFilePath = Path.Combine(LogFilesDirectoryPath, $"{Guid.NewGuid()}.log");
        }

        public void Dispose()
        {
            if (File.Exists(logFilePath))
            {
                Log.CloseAndFlush();
                GC.Collect();
                GC.WaitForPendingFinalizers();
                Policy.Handle<IOException>()
                    .WaitAndRetry(retryCount: 10, sleepDurationProvider: _ => TimeSpan.FromSeconds(1))
                    .Execute(() => File.Delete(logFilePath));
            }
        }

        [Fact]
        public async Task WhenTimeoutIsZero_ThenWriteEveryRepeatedLogAsRepeatedMessage()
        {
            var logger = new LoggerConfiguration()
                .WriteTo
                .WhenRepeated(
                    configureWrappedSink: x => x.Async(y => y.File(
                        path: logFilePath,
                        rollingInterval: RollingInterval.Infinite)),
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

            logger.Information($"{DefaultMessage}");
            logger.Information($"{DefaultMessage}");
            logger.Information($"{DefaultMessage}");
            (logger as IDisposable)?.Dispose();

            var lines = await Policy.Handle<IOException>().OrResult<string[]>(x => x.Length == 0)
                .WaitAndRetryAsync(retryCount: 10, sleepDurationProvider: _ => TimeSpan.FromSeconds(1))
                .ExecuteAsync(async () => await FileUtils.ReadAllLinesSafeAsync(logFilePath));
            lines.Should().ContainSingle(x => x.EndsWith(DefaultMessage));
            lines.Count(x => x.EndsWith(RepeatedMessage)).Should().Be(2);
        }


        [Fact]
        public async Task WhenTimeoutIsOneSecond_ThenIgnoreRepeatedMessagesLoggedEarlierThanOneSecondAgo()
        {
            var logger = new LoggerConfiguration()
                .WriteTo
                .WhenRepeated(
                    configureWrappedSink: x => x.Async(y => y.File(
                        path: logFilePath,
                        rollingInterval: RollingInterval.Infinite)),
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
                        }))
                .CreateLogger();

            logger.Information(DefaultMessage);
            logger.Information(DefaultMessage);
            await Task.Delay(TimeSpan.FromSeconds(1.1));
            logger.Information(DefaultMessage);
            (logger as IDisposable)?.Dispose();

            var lines = await Policy.Handle<IOException>().OrResult<string[]>(x => x.Length == 0)
                .WaitAndRetryAsync(retryCount: 10, sleepDurationProvider: _ => TimeSpan.FromSeconds(1))
                .ExecuteAsync(async () => await FileUtils.ReadAllLinesSafeAsync(logFilePath));
            lines.Should().ContainSingle(x => x.EndsWith(DefaultMessage));
            lines.Should().ContainSingle(x => x.EndsWith(RepeatedMessage));
        }

        [Fact]
        public async Task WhenOnRepeatCallbackReturnsNull_ThenLogOnlyFirstMessage()
        {
            var logger = new LoggerConfiguration()
                .WriteTo
                .WhenRepeated(
                    configureWrappedSink: x => x.Async(y => y.File(
                        path: logFilePath,
                        rollingInterval: RollingInterval.Infinite)),
                    options: new WhenRepeatedOptions(
                        timeout: (_, __) => TimeSpan.FromSeconds(1),
                        onRepeat: (_, __) => null))
                .CreateLogger();

            logger.Information(DefaultMessage);
            logger.Information(DefaultMessage);
            logger.Information(DefaultMessage);
            (logger as IDisposable)?.Dispose();

            var lines = await Policy.Handle<IOException>().OrResult<string[]>(x => x.Length == 0)
                .WaitAndRetryAsync(retryCount: 10, sleepDurationProvider: _ => TimeSpan.FromSeconds(1))
                .ExecuteAsync(async () => await FileUtils.ReadAllLinesSafeAsync(logFilePath));
            lines.Should().ContainSingle(x => x.EndsWith(DefaultMessage));
        }

        [Fact]
        public async Task GivenFirstStrategyIsWhenRepeated_WhenMessageLoggedTwice_ThenDontLogAnyMessage()
        {
            var logger = new LoggerConfiguration()
                .WriteTo
                .WhenRepeated(
                    configureWrappedSink: x => x.Async(y => y.File(
                        path: logFilePath,
                        rollingInterval: RollingInterval.Infinite)),
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
            await Task.Delay(TimeSpan.FromSeconds(1));
            logger.Information(RepeatedMessage);
            logger.Information(DefaultMessage);
            logger.Information(DefaultMessage);
            (logger as IDisposable)?.Dispose();

            var lines = await Policy.Handle<IOException>().OrResult<string[]>(resultPredicate: x => x.Length == 0)
                .WaitAndRetryAsync(retryCount: 10, sleepDurationProvider: _ => TimeSpan.FromSeconds(1))
                .ExecuteAsync(async () => await FileUtils.ReadAllLinesSafeAsync(logFilePath));
            lines
                .Count(x => x.EndsWith(DefaultMessage))
                .Should().Be(0, $"'{DefaultMessage}' has been logged second time in shorter time than 1 second");
        }

        [Fact]
        public async Task GivenFirstStrategyIsWhenRepeated_WhenMessageLoggedSecondTimeAfterTimeout_ThenLogMessage()
        {
            var logger = new LoggerConfiguration()
                .WriteTo
                .WhenRepeated(
                    configureWrappedSink: x => x.Async(y => y.File(
                        path: logFilePath,
                        rollingInterval: RollingInterval.Infinite)),
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
            await Task.Delay(TimeSpan.FromSeconds(1.1));
            logger.Information(DefaultMessage);
            (logger as IDisposable)?.Dispose();

            var lines = await Policy.Handle<IOException>().OrResult<string[]>(resultPredicate: x => x.Length == 0)
                .WaitAndRetryAsync(retryCount: 10, sleepDurationProvider: _ => TimeSpan.FromSeconds(1))
                .ExecuteAsync(async () => await FileUtils.ReadAllLinesSafeAsync(logFilePath));
            lines.Should().ContainSingle(x => x.EndsWith(DefaultMessage));
        }

        [Fact]
        public async Task WhenFirstStrategyIsAsRepeated_ThenInvokeOnRepeatEvenOnFirstLog()
        {
            var logger = new LoggerConfiguration()
                .WriteTo
                .WhenRepeated(
                    configureWrappedSink: x => x.Async(y => y.File(
                        path: logFilePath,
                        rollingInterval: RollingInterval.Infinite)),
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
            (logger as IDisposable)?.Dispose();

            var lines = await Policy.Handle<IOException>().OrResult<string[]>(x => x.Length == 0)
                .WaitAndRetryAsync(retryCount: 10, sleepDurationProvider: _ => TimeSpan.FromSeconds(1))
                .ExecuteAsync(async () => await FileUtils.ReadAllLinesSafeAsync(logFilePath));
            lines.Should().ContainSingle(x => x.EndsWith(RepeatedMessage));
        }

        [Fact]
        public async Task WhenFirstStrategyIsIgnore_ThenLogSecondMessage()
        {
            var logger = new LoggerConfiguration()
                .WriteTo
                .WhenRepeated(
                    configureWrappedSink: x => x.Async(y => y.File(
                        path: logFilePath,
                        rollingInterval: RollingInterval.Infinite)),
                    options: new WhenRepeatedOptions(
                        firstStrategy: (_, __) => OnFirstStrategy.Ignore,
                        timeout: (_, __) => TimeSpan.Zero))
                .CreateLogger();

            logger.Information(DefaultMessage);
            logger.Information(DefaultMessage);
            (logger as IDisposable)?.Dispose();

            var lines = await Policy.Handle<IOException>().OrResult<string[]>(x => x.Length == 0)
                .WaitAndRetryAsync(retryCount: 10, sleepDurationProvider: _ => TimeSpan.FromSeconds(1))
                .ExecuteAsync(async () => await FileUtils.ReadAllLinesSafeAsync(logFilePath));
            lines.Should().ContainSingle(x => x.EndsWith(DefaultMessage));
        }

        [Fact]
        public async Task WhenEncrichWithRepeatedMessagesCountEnabled_ThenRepeatCountAvailableInTemplate()
        {
            var logger = new LoggerConfiguration()
                .Enrich.WithRepeatedMessagesCount("repeatCount")
                .WriteTo
                .WhenRepeated(
                    configureWrappedSink: x => x.Async(y => y.File(
                        path: logFilePath,
                        rollingInterval: RollingInterval.Infinite)),
                    options: new WhenRepeatedOptions(
                        firstStrategy: (_, __) => OnFirstStrategy.Default,
                        timeout: (_, __) => TimeSpan.Zero))
                .CreateLogger();

            logger.Information($"{RepeatedMessage} {{repeatCount}}");
            logger.Information($"{RepeatedMessage} {{repeatCount}}");
            logger.Information($"{RepeatedMessage} {{repeatCount}}");
            logger.Information($"{DefaultMessage} {{repeatCount}}");
            (logger as IDisposable)?.Dispose();

            var lines = await Policy.Handle<IOException>().OrResult<string[]>(x => x.Length != 4)
                .WaitAndRetryAsync(retryCount: 10, sleepDurationProvider: _ => TimeSpan.FromSeconds(1))
                .ExecuteAsync(async () => await FileUtils.ReadAllLinesSafeAsync(logFilePath));
            lines[0].Should().EndWith($"{RepeatedMessage} \"0\"");
            lines[1].Should().EndWith($"{RepeatedMessage} \"1\"");
            lines[2].Should().EndWith($"{RepeatedMessage} \"2\"");
            lines[3].Should().EndWith($"{DefaultMessage} \"0\"");
        }
    }
}