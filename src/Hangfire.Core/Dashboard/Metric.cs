// This file is part of Hangfire. Copyright © 2015 Sergey Odinokov.
// 
// Permission to use, copy, modify, and/or distribute this software for any
// purpose with or without fee is hereby granted.
// 
// THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES WITH
// REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY
// AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY SPECIAL, DIRECT,
// INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM
// LOSS OF USE, DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR
// OTHER TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR
// PERFORMANCE OF THIS SOFTWARE.

namespace Hangfire.Dashboard
{
    public class Metric
    {
        public Metric(string value)
        {
            Value = value;
        }

        public Metric(long value)
        {
            Value = value.ToString("N0");
            IntValue = value;
        }

        public string Value { get; }
        public long IntValue { get; set; }
        public MetricStyle Style { get; set; }
        public bool Highlighted { get; set; }
        public string Title { get; set; }
    }

    public enum MetricStyle
    {
        Default,
        Info,
        Success,
        Warning,
        Danger,
    }

    internal static class MetricStyleExtensions
    {
        public static string ToClassName(this MetricStyle style)
        {
            switch (style)
            {
                case MetricStyle.Default: return "metric-default";
                case MetricStyle.Info:    return "metric-info";
                case MetricStyle.Success: return "metric-success";
                case MetricStyle.Warning: return "metric-warning";
                case MetricStyle.Danger:  return "metric-danger";
                default:                  return "metric-null";
            }
        }
    }
}
