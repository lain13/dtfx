using System;
using System.Collections.Generic;
using System.IO;
using Serilog.Events;
using Serilog.Formatting;

namespace IF.Batch.Common.Diagnostics
{
    /// <summary>
    /// Keeps the established DTFX CSV layout while Serilog owns event processing and file output.
    /// </summary>
    internal sealed class SerilogCsvFormatter : ITextFormatter
    {
        public void Format(LogEvent logEvent, TextWriter output)
        {
            var fields = new List<string>
            {
                logEvent.Timestamp.LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss"),
                ToTraceEventType(logEvent.Level),
                GetScalar(logEvent, "ThreadId")
            };

            string method = GetScalar(logEvent, "Method");
            if (!string.IsNullOrEmpty(method))
            {
                fields.Add(method);
            }

            fields.Add(logEvent.RenderMessage());
            if (logEvent.Exception != null)
            {
                fields.Add(logEvent.Exception.ToString());
            }

            for (int i = 0; i < fields.Count; i++)
            {
                if (i > 0)
                {
                    output.Write(',');
                }
                WriteCsvField(output, fields[i]);
            }
            output.WriteLine();
        }

        private static string GetScalar(LogEvent logEvent, string name)
        {
            LogEventPropertyValue value;
            if (!logEvent.Properties.TryGetValue(name, out value))
            {
                return string.Empty;
            }

            var scalar = value as ScalarValue;
            return scalar == null || scalar.Value == null ? string.Empty : Convert.ToString(scalar.Value);
        }

        private static string ToTraceEventType(LogEventLevel level)
        {
            switch (level)
            {
                case LogEventLevel.Fatal:
                    return "Critical";
                case LogEventLevel.Error:
                    return "Error";
                case LogEventLevel.Warning:
                    return "Warning";
                case LogEventLevel.Information:
                    return "Information";
                default:
                    return "Verbose";
            }
        }

        private static void WriteCsvField(TextWriter output, string value)
        {
            value = value ?? string.Empty;
            bool quote = value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0 || value.Trim() != value;
            if (!quote)
            {
                output.Write(value);
                return;
            }

            output.Write('"');
            output.Write(value.Replace("\"", "\"\""));
            output.Write('"');
        }
    }
}
