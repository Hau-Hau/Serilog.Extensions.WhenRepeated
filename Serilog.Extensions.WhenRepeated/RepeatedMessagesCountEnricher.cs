using Serilog.Core;
using Serilog.Events;

namespace Serilog.Extensions.WhenRepeated
{
    internal class RepeatedMessagesCountEnricher : ILogEventEnricher
    {
        private readonly string propertyName;

        public RepeatedMessagesCountEnricher(string propertyName)
        {
            this.propertyName = propertyName;
        }

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            if (!logEvent.Properties.ContainsKey(Constants.RepeatedMessagesCountPropertyNameProperty))
            {
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(Constants.RepeatedMessagesCountPropertyNameProperty, propertyName));
            }
        }
    }
}
