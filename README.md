# Serilog.Extensions.WhenRepeated
[![tests](https://github.com/Hau-Hau/Serilog.Extensions.WhenRepeated/actions/workflows/tests.yml/badge.svg)](https://github.com/Hau-Hau/Serilog.Extensions.WhenRepeated/actions/workflows/tests.yml)
[![Coverage Status](https://coveralls.io/repos/github/Hau-Hau/Serilog.Extensions.WhenRepeated/badge.svg)](https://coveralls.io/github/Hau-Hau/Serilog.Extensions.WhenRepeated)


A wrapper for other Serilog sinks. Matches when message has already been logged. Useful if having an aggressive logger, and wants to throttle the output.

### Getting started

# Install from [NuGet](https://nuget.org/packages/TODO):

```powershell
Install-Package TODO
```

### Minimal example
```csharp
// At this example duplicated messages are written as as dot
...
private static readonly MessageTemplate RepeatedMessageTemplate = new MessageTemplate(new MessageTemplateParser().Parse(".").Tokens);

new LoggerConfiguration()
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