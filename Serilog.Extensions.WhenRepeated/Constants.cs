using System;

namespace Serilog.Extensions.WhenRepeated
{
    internal static class Constants
    {
        internal const string RepeatedMessagesCountPropertyNameProperty = "__WhenRepeated__RepeatedMessagesCountPropertyName__";
        internal static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);
    }
}
