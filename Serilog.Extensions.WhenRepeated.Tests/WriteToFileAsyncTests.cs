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
        public static readonly string LogFilePath = Path.Combine(Path.GetTempPath(), "Serilog.Extensions.WhenRepeated.Tests.log");

        public WriteToFileAsyncTests()
        {
            if (File.Exists(LogFilePath))
            {
                Log.CloseAndFlush();
                Policy.Handle<IOException>()
                    .WaitAndRetry(retryCount: 10, sleepDurationProvider: _ => TimeSpan.FromSeconds(1))
                    .Execute(() => File.Delete(LogFilePath));
            }
        }

        public void Dispose()
        {
            if (File.Exists(LogFilePath))
            {
                Log.CloseAndFlush();
                GC.Collect();
                GC.WaitForPendingFinalizers();
                Policy.Handle<IOException>()
                    .WaitAndRetry(retryCount: 10, sleepDurationProvider: _ => TimeSpan.FromSeconds(1))
                    .Execute(() => File.Delete(LogFilePath));
            }
        }

        [Fact]
        public async Task WhenTimeoutIsZero_ThenWriteEveryRepeatedLogAsRepeatedMessage()
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo
                .WhenRepeated(
                    configureWrappedSink: x => x.Async(y => y.File(
                        path: LogFilePath,
                        rollingInterval: RollingInterval.Infinite)),
                    options: new WhenRepeatedOptions(
                        timeout: (_, _) => TimeSpan.Zero,
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

            Log.Logger.Information($"{DefaultMessage}");
            Log.Logger.Information($"{DefaultMessage}");
            Log.Logger.Information($"{DefaultMessage}");

            var lines = await Policy.Handle<IOException>().OrResult<string[]>(x => x.Length == 0)
                .WaitAndRetryAsync(retryCount: 10, sleepDurationProvider: _ => TimeSpan.FromSeconds(1))
                .ExecuteAsync(async () => await FileUtils.ReadAllLinesSafeAsync(LogFilePath));
            lines.Should().ContainSingle(x => x.EndsWith(DefaultMessage));
            lines.Count(x => x.EndsWith(RepeatedMessage)).Should().Be(2);
        }


        [Fact]
        public async Task WhenTimeoutIsOneSecond_ThenIgnoreRepeatedMessagesLoggedEarlierThanOneSecondAgo()
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo
                .WhenRepeated(
                    configureWrappedSink: x => x.Async(y => y.File(
                        path: LogFilePath,
                        rollingInterval: RollingInterval.Infinite)),
                    options: new WhenRepeatedOptions(
                        timeout: (_, _) => TimeSpan.FromSeconds(1),
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

            Log.Logger.Information(DefaultMessage);
            Log.Logger.Information(DefaultMessage);
            await Task.Delay(TimeSpan.FromSeconds(1));
            Log.Logger.Information(DefaultMessage);

            var lines = await Policy.Handle<IOException>().OrResult<string[]>(x => x.Length == 0)
                .WaitAndRetryAsync(retryCount: 10, sleepDurationProvider: _ => TimeSpan.FromSeconds(1))
                .ExecuteAsync(async () => await FileUtils.ReadAllLinesSafeAsync(LogFilePath));
            lines.Should().ContainSingle(x => x.EndsWith(DefaultMessage));
            lines.Should().ContainSingle(x => x.EndsWith(RepeatedMessage));
        }

        [Fact]
        public async Task WhenOnRepeatCallbackReturnsNull_ThenLogOnlyFirstMessage()
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo
                .WhenRepeated(
                    configureWrappedSink: x => x.Async(y => y.File(
                        path: LogFilePath,
                        rollingInterval: RollingInterval.Infinite)),
                    options: new WhenRepeatedOptions(
                        timeout: (_, _) => TimeSpan.FromSeconds(1),
                        onRepeat: (_, _) => null))
                .CreateLogger();

            Log.Logger.Information(DefaultMessage);
            Log.Logger.Information(DefaultMessage);
            Log.Logger.Information(DefaultMessage);

            var lines = await Policy.Handle<IOException>().OrResult<string[]>(x => x.Length == 0)
                .WaitAndRetryAsync(retryCount: 10, sleepDurationProvider: _ => TimeSpan.FromSeconds(1))
                .ExecuteAsync(async () => await FileUtils.ReadAllLinesSafeAsync(LogFilePath));
            lines.Should().ContainSingle(x => x.EndsWith(DefaultMessage));
        }

        [Fact]
        public async Task GivenFirstStrategyIsWhenRepeated_WhenMessageLoggedTwice_ThenDontLogAnyMessage()
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo
                .WhenRepeated(
                    configureWrappedSink: x => x.Async(y => y.File(
                        path: LogFilePath,
                        rollingInterval: RollingInterval.Infinite)),
                    options: new WhenRepeatedOptions(
                        firstStrategy: (_, _) => OnFirstStrategy.WhenRepeated,
                        timeout: (_, _) => TimeSpan.FromSeconds(1),
                        onRepeat: (current, _) => new LogEvent(
                                timestamp: current.Timestamp,
                                level: current.Level,
                                exception: null,
                                messageTemplate: RepeatedMessageTemplate,
                                properties: Array.Empty<LogEventProperty>())))
                .CreateLogger();

            Log.Logger.Information(DefaultMessage);
            Log.Logger.Information(DefaultMessage);

            await Task.Delay(TimeSpan.FromSeconds(1));
            var lines = await Policy.Handle<IOException>()
                .WaitAndRetryAsync(retryCount: 10, sleepDurationProvider: _ => TimeSpan.FromSeconds(1))
                .ExecuteAsync(async () => await FileUtils.ReadAllLinesSafeAsync(LogFilePath));
            lines.Count(x => x == DefaultMessage)
                .Should().Be(0, $"'{DefaultMessage}' has been logged second time in shorter time than 1 second");

            await Task.Delay(TimeSpan.FromSeconds(1));
            Log.Logger.Information(RepeatedMessage);

            Log.Logger.Information(DefaultMessage);
            Log.Logger.Information(DefaultMessage);

            await Task.Delay(TimeSpan.FromSeconds(1));
            lines = await Policy.Handle<IOException>().OrResult<string[]>(resultPredicate: x => x.Length == 0)
                .WaitAndRetryAsync(retryCount: 10, sleepDurationProvider: _ => TimeSpan.FromSeconds(1))
                .ExecuteAsync(async () => await FileUtils.ReadAllLinesSafeAsync(LogFilePath));
            lines
                .Count(x => x.EndsWith(DefaultMessage))
                .Should().Be(0, $"'{DefaultMessage}' has been logged second time in shorter time than 1 second and has been preceded by '{RepeatedMessage}' message");
        }

        [Fact]
        public async Task GivenFirstStrategyIsWhenRepeated_WhenMessageLoggedSecondTimeAfterTimeout_ThenLogMessage()
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo
                .WhenRepeated(
                    configureWrappedSink: x => x.Async(y => y.File(
                        path: LogFilePath,
                        rollingInterval: RollingInterval.Infinite)),
                    options: new WhenRepeatedOptions(
                        firstStrategy: (_, _) => OnFirstStrategy.WhenRepeated,
                        timeout: (_, _) => TimeSpan.FromSeconds(1),
                        onRepeat: (current, _) => new LogEvent(
                                timestamp: current.Timestamp,
                                level: current.Level,
                                exception: null,
                                messageTemplate: RepeatedMessageTemplate,
                                properties: Array.Empty<LogEventProperty>())))
                .CreateLogger();

            Log.Logger.Information(DefaultMessage);
            await Task.Delay(TimeSpan.FromSeconds(1));
            Log.Logger.Information(DefaultMessage);

            await Task.Delay(TimeSpan.FromSeconds(1));
            var lines = await Policy.Handle<IOException>().OrResult<string[]>(resultPredicate: x => x.Length == 0)
                .WaitAndRetryAsync(retryCount: 10, sleepDurationProvider: _ => TimeSpan.FromSeconds(1))
                .ExecuteAsync(async () => await FileUtils.ReadAllLinesSafeAsync(LogFilePath));
            lines.Should().ContainSingle(x => x.EndsWith(DefaultMessage));
        }

        [Fact]
        public async Task WhenFirstStrategyIsAsRepeated_ThenInvokeOnRepeatEvenOnFirstLog()
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo
                .WhenRepeated(
                    configureWrappedSink: x => x.Async(y => y.File(
                        path: LogFilePath,
                        rollingInterval: RollingInterval.Infinite)),
                    options: new WhenRepeatedOptions(
                        firstStrategy: (_, _) => OnFirstStrategy.AsRepeated,
                        timeout: (_, _) => TimeSpan.FromSeconds(1),
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

            Log.Logger.Information(DefaultMessage);

            var lines = await Policy.Handle<IOException>().OrResult<string[]>(x => x.Length == 0)
                .WaitAndRetryAsync(retryCount: 10, sleepDurationProvider: _ => TimeSpan.FromSeconds(1))
                .ExecuteAsync(async () => await FileUtils.ReadAllLinesSafeAsync(LogFilePath));
            lines.Should()
                .ContainSingle(x => x.EndsWith(RepeatedMessage));
        }

        [Fact]
        public async Task WhenFirstStrategyIsIgnore_ThenLogSecondMessage()
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo
                .WhenRepeated(
                    configureWrappedSink: x => x.Async(y => y.File(
                        path: LogFilePath,
                        rollingInterval: RollingInterval.Infinite)),
                    options: new WhenRepeatedOptions(
                        firstStrategy: (_, _) => OnFirstStrategy.Ignore,
                        timeout: (_, _) => TimeSpan.Zero))
                .CreateLogger();

            Log.Logger.Information(DefaultMessage);
            Log.Logger.Information(DefaultMessage);

            var lines = await Policy.Handle<IOException>().OrResult<string[]>(x => x.Length == 0)
                .WaitAndRetryAsync(retryCount: 10, sleepDurationProvider: _ => TimeSpan.FromSeconds(1))
                .ExecuteAsync(async () => await FileUtils.ReadAllLinesSafeAsync(LogFilePath));
            lines.Should()
                .ContainSingle(x => x.EndsWith(DefaultMessage));
        }

        [Fact]
        public async Task WhenEncrichWithRepeatedMessagesCountEnabled_ThenRepeatCountAvailableInTemplate()
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.WithRepeatedMessagesCount("repeatCount")
                .WriteTo
                .WhenRepeated(
                    configureWrappedSink: x => x.Async(y => y.File(
                        path: LogFilePath,
                        rollingInterval: RollingInterval.Infinite)),
                    options: new WhenRepeatedOptions(
                        firstStrategy: (_, _) => OnFirstStrategy.Default,
                        timeout: (_, _) => TimeSpan.Zero))
                .CreateLogger();

            Log.Logger.Information($"{RepeatedMessage} {{repeatCount}}");
            var lines = await Policy.Handle<IOException>().OrResult<string[]>(x => x.Length == 0)
                .WaitAndRetryAsync(retryCount: 10, sleepDurationProvider: _ => TimeSpan.FromSeconds(1))
                .ExecuteAsync(async () => await FileUtils.ReadAllLinesSafeAsync(LogFilePath));
            lines.Last().Should().EndWith($"{RepeatedMessage} \"0\"");

            Log.Logger.Information($"{RepeatedMessage} {{repeatCount}}");
            await Task.Delay(TimeSpan.FromSeconds(1));
            lines = await Policy.Handle<IOException>().OrResult<string[]>(x => x.Length == 0)
                .WaitAndRetryAsync(retryCount: 10, sleepDurationProvider: _ => TimeSpan.FromSeconds(1))
                .ExecuteAsync(async () => await FileUtils.ReadAllLinesSafeAsync(LogFilePath));
            lines.Last().Should().EndWith($"{RepeatedMessage} \"1\"");

            Log.Logger.Information($"{RepeatedMessage} {{repeatCount}}");
            await Task.Delay(TimeSpan.FromSeconds(1));
            lines = await Policy.Handle<IOException>().OrResult<string[]>(x => x.Length == 0)
                .WaitAndRetryAsync(retryCount: 10, sleepDurationProvider: _ => TimeSpan.FromSeconds(1))
                .ExecuteAsync(async () => await FileUtils.ReadAllLinesSafeAsync(LogFilePath));
            lines.Last().Should().EndWith($"{RepeatedMessage} \"2\"");

            Log.Logger.Information($"{DefaultMessage} {{repeatCount}}");
            await Task.Delay(TimeSpan.FromSeconds(1));
            lines = await Policy.Handle<IOException>().OrResult<string[]>(x => x.Length == 0)
                .WaitAndRetryAsync(retryCount: 10, sleepDurationProvider: _ => TimeSpan.FromSeconds(1))
                .ExecuteAsync(async () => await FileUtils.ReadAllLinesSafeAsync(LogFilePath));
            lines.Last().Should().EndWith($"{DefaultMessage} \"0\"");
        }
    }
}