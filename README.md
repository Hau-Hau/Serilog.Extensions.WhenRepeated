# Serilog.Extensions.WhenRepeated
[![tests](https://github.com/Hau-Hau/Serilog.Extensions.WhenRepeated/actions/workflows/tests.yml/badge.svg)](https://github.com/Hau-Hau/Serilog.Extensions.WhenRepeated/actions/workflows/tests.yml)
[![Coverage Status](https://coveralls.io/repos/github/Hau-Hau/Serilog.Extensions.WhenRepeated/badge.svg)](https://coveralls.io/github/Hau-Hau/Serilog.Extensions.WhenRepeated)


A wrapper for other Serilog sinks. Matches when message has already been logged. Useful if having an aggressive logger, and wants to throttle the output.

### Getting started

#### Install from [NuGet](https://www.nuget.org/packages/Serilog.Extensions.WhenRepeated):

```powershell
Install-Package Serilog.Extensions.WhenRepeated 
```

### Minimal example
```csharp
// At this example duplicated messages are written as as dot
...
private static readonly MessageTemplate RepeatedMessageTemplate = new MessageTemplate(new MessageTemplateParser().Parse(".").Tokens);

new LoggerConfiguration()
  .Enrich.WithRepeatedMessagesCount("repeatCount")
  .WriteTo
  .WhenRepeated(
    configureWrappedSink: x => x.Async(y.File("/path/to/log.txt")),
    options: new WhenRepeatedOptions(
      onRepeat: x =>
      {
        return new LogEvent(
          timestamp: x.Timestamp,
          level: x.Level,
          exception: null,
          messageTemplate: RepeatedMessageTemplate,
          properties: Array.Empty<LogEventProperty>());
      })).CreateLogger();
...
```

### Options
The WhenRepeatedOptions type constructor exposes the following members.

| Name  | Description |
| ------------- | ------------- |
| onRepeat  | Action to be taken when filter matches.  |
| compare  | Statement of that how to compare current log with previous log event.  |
| timeout  | Duration after which message if duplicated will be logged.  |
| firstStrategy  | Strategy that determines how to handle first (not duplicated) log event.  |

### Enrichment
Enrich Serilog events with repeated messages count property.

```csharp
...
new LoggerConfiguration()
  .Enrich.WithRepeatedMessagesCount("repeatCount") // Set name under which property will be available
  .WriteTo
  .WhenRepeated(
    configureWrappedSink: x => x.Async(y.File("/path/to/log.txt")),
    options: new WhenRepeatedOptions() // Default options, needed to enable counting. By default increment repeated messages count when same message occurs in 10 seconds time interval.
  ).CreateLogger();
...
logger.Information(messageTemplate: "{repeatCount}");
```

