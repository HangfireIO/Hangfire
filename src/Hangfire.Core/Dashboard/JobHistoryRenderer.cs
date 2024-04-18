// This file is part of Hangfire. Copyright © 2013-2014 Hangfire OÜ.
// 
// Hangfire is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as 
// published by the Free Software Foundation, either version 3 
// of the License, or any later version.
// 
// Hangfire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public 
// License along with Hangfire. If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Hangfire.Common;
using Hangfire.States;

namespace Hangfire.Dashboard
{
    public static class JobHistoryRenderer
    {
        private static readonly IDictionary<string, Func<HtmlHelper, IDictionary<string, string>, NonEscapedString>>
            Renderers = new Dictionary<string, Func<HtmlHelper, IDictionary<string, string>, NonEscapedString>>();

        private static readonly IDictionary<string, string> BackgroundStateColors
            = new Dictionary<string, string>();
        private static readonly IDictionary<string, string> ForegroundStateColors
            = new Dictionary<string, string>();
        private static readonly IDictionary<string, string> StateCssSuffixes
            = new Dictionary<string, string>();

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline")]
        static JobHistoryRenderer()
        {
            Register(SucceededState.StateName, SucceededRenderer);
            Register(FailedState.StateName, FailedRenderer);
            Register(ProcessingState.StateName, ProcessingRenderer);
            Register(EnqueuedState.StateName, EnqueuedRenderer);
            Register(ScheduledState.StateName, ScheduledRenderer);
            Register(DeletedState.StateName, DeletedRenderer);
            Register(AwaitingState.StateName, AwaitingRenderer);

            BackgroundStateColors.Add(EnqueuedState.StateName, "#F5F5F5");
            BackgroundStateColors.Add(SucceededState.StateName, "#EDF7ED");
            BackgroundStateColors.Add(FailedState.StateName, "#FAEBEA");
            BackgroundStateColors.Add(ProcessingState.StateName, "#FCEFDC");
            BackgroundStateColors.Add(ScheduledState.StateName, "#E0F3F8");
            BackgroundStateColors.Add(DeletedState.StateName, "#ddd");
            BackgroundStateColors.Add(AwaitingState.StateName, "#E0F3F8");

            ForegroundStateColors.Add(EnqueuedState.StateName, "#999");
            ForegroundStateColors.Add(SucceededState.StateName, "#5cb85c");
            ForegroundStateColors.Add(FailedState.StateName, "#d9534f");
            ForegroundStateColors.Add(ProcessingState.StateName, "#f0ad4e");
            ForegroundStateColors.Add(ScheduledState.StateName, "#5bc0de");
            ForegroundStateColors.Add(DeletedState.StateName, "#777");
            ForegroundStateColors.Add(AwaitingState.StateName, "#5bc0de");

            StateCssSuffixes.Add(EnqueuedState.StateName, "active");
            StateCssSuffixes.Add(SucceededState.StateName, "success");
            StateCssSuffixes.Add(FailedState.StateName, "danger");
            StateCssSuffixes.Add(ProcessingState.StateName, "warning");
            StateCssSuffixes.Add(ScheduledState.StateName, "info");
            StateCssSuffixes.Add(DeletedState.StateName, "inactive");
            StateCssSuffixes.Add(AwaitingState.StateName, "info");
        }

        [Obsolete("Use `AddStateCssSuffix` method's logic instead. Will be removed in 2.0.0.")]
        public static void AddBackgroundStateColor(string stateName, string color)
        {
            BackgroundStateColors.Add(stateName, color);
        }

        public static string GetBackgroundStateColor(string stateName)
        {
            if (stateName == null || !BackgroundStateColors.TryGetValue(stateName, out var color))
            {
                return "inherit";
            }

            return color;
        }

        [Obsolete("Use `AddStateCssSuffix` method's logic instead. Will be removed in 2.0.0.")]
        public static void AddForegroundStateColor(string stateName, string color)
        {
            ForegroundStateColors.Add(stateName, color);
        }

        public static string GetForegroundStateColor(string stateName)
        {
            if (stateName == null || !ForegroundStateColors.TryGetValue(stateName, out var color))
            {
                return "inherit";
            }

            return color;
        }

        public static void AddStateCssSuffix(string stateName, string color)
        {
            StateCssSuffixes.Add(stateName, color);
        }

        public static string GetStateCssSuffix(string stateName)
        {
            if (stateName == null || !StateCssSuffixes.TryGetValue(stateName, out var suffix))
            {
                return "inherit";
            }

            return suffix;
        }

        public static void Register(string state, Func<HtmlHelper, IDictionary<string, string>, NonEscapedString> renderer)
        {
            if (!Renderers.ContainsKey(state))
            {
                Renderers.Add(state, renderer);
            }
            else
            {
                Renderers[state] = renderer;
            }
        }

        public static bool Exists(string state)
        {
            return Renderers.ContainsKey(state);
        }

        public static NonEscapedString RenderHistory(
            this HtmlHelper helper,
            string state, IDictionary<string, string> properties)
        {
            var renderer = Renderers.TryGetValue(state, out var value)
                ? value
                : DefaultRenderer;

            return renderer?.Invoke(helper, properties);
        }

        public static NonEscapedString NullRenderer(HtmlHelper helper, IDictionary<string, string> properties)
        {
            return null;
        }

        public static NonEscapedString DefaultRenderer(HtmlHelper helper, IDictionary<string, string> stateData)
        {
            if (stateData == null || stateData.Count == 0) return null;

            var builder = new StringBuilder();
            builder.Append("<dl class=\"dl-horizontal\">");

            foreach (var item in stateData)
            {
                builder.Append($"<dt>{helper.HtmlEncode(item.Key)}</dt>");
                builder.Append($"<dd>{helper.HtmlEncode(item.Value)}</dd>");
            }

            builder.Append("</dl>");

            return new NonEscapedString(builder.ToString());
        }

        public static NonEscapedString SucceededRenderer(HtmlHelper html, IDictionary<string, string> stateData)
        {
            var builder = new StringBuilder();
            builder.Append("<dl class=\"dl-horizontal\">");

            var itemsAdded = false;

            if (stateData.TryGetValue("Latency", out var latencyString))
            {
                var latency = TimeSpan.FromMilliseconds(long.Parse(latencyString, CultureInfo.InvariantCulture));

                builder.Append($"<dt>Latency:</dt><dd>{html.HtmlEncode(html.ToHumanDuration(latency, false))}</dd>");

                itemsAdded = true;
            }

            if (stateData.TryGetValue("PerformanceDuration", out var durationString))
            {
                var duration = TimeSpan.FromMilliseconds(long.Parse(durationString, CultureInfo.InvariantCulture));
                builder.Append($"<dt>Duration:</dt><dd>{html.HtmlEncode(html.ToHumanDuration(duration, false))}</dd>");

                itemsAdded = true;
            }


            if (stateData.TryGetValue("Result", out var resultString) && !String.IsNullOrWhiteSpace(resultString))
            {
                var result = stateData["Result"].PrettyJsonString();
                builder.Append($"<dt>Result:</dt><dd>{html.HtmlEncode(result).Replace("\n", "<br>")}</dd>");

                itemsAdded = true;
            }

            builder.Append("</dl>");

            if (!itemsAdded) return null;

            return new NonEscapedString(builder.ToString());
        }

        private static NonEscapedString FailedRenderer(HtmlHelper html, IDictionary<string, string> stateData)
        {
            var builder = new StringBuilder();
            var serverId = stateData.TryGetValue("ServerId", out var value) ? $" ({html.ServerId(value)})" : null;

            builder.Append(
                $"<h4 class=\"exception-type\">{html.HtmlEncode(stateData["ExceptionType"])}{serverId}</h4><p class=\"text-muted\">{html.HtmlEncode(stateData["ExceptionMessage"])}</p>");

            if (stateData.TryGetValue("ExceptionDetails", out var details) && !String.IsNullOrWhiteSpace(details))
            {
                var stackTrace = html.StackTrace(details).ToString();
                builder.Append($"<pre class=\"stack-trace\">{stackTrace}</pre>");
            }

            return new NonEscapedString(builder.ToString());
        }

        private static NonEscapedString ProcessingRenderer(HtmlHelper helper, IDictionary<string, string> stateData)
        {
            var builder = new StringBuilder();
            builder.Append("<dl class=\"dl-horizontal\">");

            if (!stateData.TryGetValue("ServerId", out var serverId))
            {
                stateData.TryGetValue("ServerName", out serverId);
            }

            if (serverId != null)
            {
                builder.Append("<dt>Server:</dt>");
                builder.Append($"<dd>{helper.ServerId(serverId)}</dd>");
            }

            if (stateData.TryGetValue("WorkerId", out var workerId))
            {
                builder.Append("<dt>Worker:</dt>");
                builder.Append($"<dd>{helper.HtmlEncode(workerId.Substring(0, 8))}</dd>");
            }
            else if (stateData.TryGetValue("WorkerNumber", out var workerNumber))
            {
                builder.Append("<dt>Worker:</dt>");
                builder.Append($"<dd>#{helper.HtmlEncode(workerNumber)}</dd>");
            }

            builder.Append("</dl>");

            return new NonEscapedString(builder.ToString());
        }

        private static NonEscapedString EnqueuedRenderer(HtmlHelper helper, IDictionary<string, string> stateData)
        {
            if (!EnqueuedState.DefaultQueue.Equals(stateData["Queue"], StringComparison.OrdinalIgnoreCase))
            {
                return new NonEscapedString(
                    $"<dl class=\"dl-horizontal\"><dt>Queue:</dt><dd>{helper.QueueLabel(stateData["Queue"])}</dd></dl>");
            }

            return null;
        }

        private static NonEscapedString ScheduledRenderer(HtmlHelper helper, IDictionary<string, string> stateData)
        {
            var enqueueAt = JobHelper.DeserializeDateTime(stateData["EnqueueAt"]);
            stateData.TryGetValue("Queue", out var queue);

            var sb = new StringBuilder();
            sb.Append("<dl class=\"dl-horizontal\">");
            sb.Append($"<dt>Enqueue at:</dt><dd data-moment=\"{helper.HtmlEncode(JobHelper.ToTimestamp(enqueueAt).ToString(CultureInfo.InvariantCulture))}\">{helper.HtmlEncode(enqueueAt.ToString(CultureInfo.CurrentCulture))}</dd>");

            if (!String.IsNullOrWhiteSpace(queue))
            {
                sb.Append($"<dt>Queue:</dt><dd>{helper.QueueLabel(queue)}</dd>");
            }

            sb.Append("</dl>");

            return new NonEscapedString(sb.ToString());
        }

        private static NonEscapedString AwaitingRenderer(HtmlHelper helper, IDictionary<string, string> stateData)
        {
            var builder = new StringBuilder();

            builder.Append("<dl class=\"dl-horizontal\">");

            if (stateData.TryGetValue("ParentId", out var parentId))
            {
                builder.Append($"<dt>Parent</dt><dd>{helper.JobIdLink(parentId)}</dd>");
            }

            if (stateData.TryGetValue("NextState", out var nextStateString))
            {
                var nextState = SerializationHelper.Deserialize<IState>(nextStateString, SerializationOption.TypedInternal);

                builder.Append($"<dt>Next State</dt><dd>{helper.StateLabel(nextState?.Name ?? "(no state)")}</dd>");
            }

            if (stateData.TryGetValue("Options", out var optionsDescription))
            {
                if (Enum.TryParse(optionsDescription, out JobContinuationOptions options))
                {
                    optionsDescription = options.ToString("G");
                }

                builder.Append($"<dt>Options</dt><dd><code>{helper.HtmlEncode(optionsDescription)}</code></dd>");
            }

            builder.Append("</dl>");

            return new NonEscapedString(builder.ToString());
        }
        
        private static NonEscapedString DeletedRenderer(HtmlHelper html, IDictionary<string, string> stateData)
        {
            if (stateData.TryGetValue("Exception", out var exception))
            {
                var exceptionInfo = SerializationHelper.Deserialize<ExceptionInfo>(exception);
                if (exceptionInfo != null)
                {
                    var commaIndex = exceptionInfo.Type.IndexOf(",", StringComparison.OrdinalIgnoreCase);
                    var typeName = commaIndex > 0 
                        ? exceptionInfo.Type.Substring(0, commaIndex)
                        : exceptionInfo.Type;

                    return new NonEscapedString(
                        $"<h4 class=\"exception-type\">{html.HtmlEncode(typeName)}</h4><p class=\"text-muted\">{html.HtmlEncode(exceptionInfo.Message)}</p>");
                }
            }

            return null;
        }
    }
}
