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
            Register(DeletedState.StateName, NullRenderer);
            Register(AwaitingState.StateName, AwaitingRenderer);

            BackgroundStateColors.Add(EnqueuedState.StateName, "#F5F5F5");
            BackgroundStateColors.Add(SucceededState.StateName, "#EDF7ED");
            BackgroundStateColors.Add(FailedState.StateName, "#FAEBEA");
            BackgroundStateColors.Add(ProcessingState.StateName, "#FCEFDC");
            BackgroundStateColors.Add(ScheduledState.StateName, "#E0F3F8");
            BackgroundStateColors.Add(DeletedState.StateName, "#ddd");
            BackgroundStateColors.Add(AwaitingState.StateName, "#F5F5F5");

            ForegroundStateColors.Add(EnqueuedState.StateName, "#999");
            ForegroundStateColors.Add(SucceededState.StateName, "#5cb85c");
            ForegroundStateColors.Add(FailedState.StateName, "#d9534f");
            ForegroundStateColors.Add(ProcessingState.StateName, "#f0ad4e");
            ForegroundStateColors.Add(ScheduledState.StateName, "#5bc0de");
            ForegroundStateColors.Add(DeletedState.StateName, "#777");
            ForegroundStateColors.Add(AwaitingState.StateName, "#999");

            StateCssSuffixes.Add(EnqueuedState.StateName, "active");
            StateCssSuffixes.Add(SucceededState.StateName, "success");
            StateCssSuffixes.Add(FailedState.StateName, "danger");
            StateCssSuffixes.Add(ProcessingState.StateName, "warning");
            StateCssSuffixes.Add(ScheduledState.StateName, "info");
            StateCssSuffixes.Add(DeletedState.StateName, "inactive");
            StateCssSuffixes.Add(AwaitingState.StateName, "active");
        }

        [Obsolete("Use `AddStateCssSuffix` method's logic instead. Will be removed in 2.0.0.")]
        public static void AddBackgroundStateColor(string stateName, string color)
        {
            BackgroundStateColors.Add(stateName, color);
        }

        public static string GetBackgroundStateColor(string stateName)
        {
            if (stateName == null || !BackgroundStateColors.ContainsKey(stateName))
            {
                return "inherit";
            }

            return BackgroundStateColors[stateName];
        }

        [Obsolete("Use `AddStateCssSuffix` method's logic instead. Will be removed in 2.0.0.")]
        public static void AddForegroundStateColor(string stateName, string color)
        {
            ForegroundStateColors.Add(stateName, color);
        }

        public static string GetForegroundStateColor(string stateName)
        {
            if (stateName == null || !ForegroundStateColors.ContainsKey(stateName))
            {
                return "inherit";
            }

            return ForegroundStateColors[stateName];
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
            var renderer = Renderers.ContainsKey(state)
                ? Renderers[state]
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

            if (stateData.ContainsKey("Latency"))
            {
                var latency = TimeSpan.FromMilliseconds(long.Parse(stateData["Latency"]));

                builder.Append($"<dt>Latency:</dt><dd>{html.HtmlEncode(html.ToHumanDuration(latency, false))}</dd>");

                itemsAdded = true;
            }

            if (stateData.ContainsKey("PerformanceDuration"))
            {
                var duration = TimeSpan.FromMilliseconds(long.Parse(stateData["PerformanceDuration"]));
                builder.Append($"<dt>Duration:</dt><dd>{html.HtmlEncode(html.ToHumanDuration(duration, false))}</dd>");

                itemsAdded = true;
            }


            if (stateData.ContainsKey("Result") && !String.IsNullOrWhiteSpace(stateData["Result"]))
            {
                var result = stateData["Result"];
                builder.Append($"<dt>Result:</dt><dd>{html.HtmlEncode(result)}</dd>");

                itemsAdded = true;
            }

            builder.Append("</dl>");

            if (!itemsAdded) return null;

            return new NonEscapedString(builder.ToString());
        }

        private static NonEscapedString FailedRenderer(HtmlHelper html, IDictionary<string, string> stateData)
        {
            var stackTrace = html.StackTrace(stateData["ExceptionDetails"]).ToString();
            return new NonEscapedString(
                $"<h4 class=\"exception-type\">{html.HtmlEncode(stateData["ExceptionType"])}</h4><p class=\"text-muted\">{html.HtmlEncode(stateData["ExceptionMessage"])}</p><pre class=\"stack-trace\">{stackTrace}</pre>");
        }

        private static NonEscapedString ProcessingRenderer(HtmlHelper helper, IDictionary<string, string> stateData)
        {
            var builder = new StringBuilder();
            builder.Append("<dl class=\"dl-horizontal\">");

            string serverId = null;

            if (stateData.ContainsKey("ServerId"))
            {
                serverId = stateData["ServerId"];
            }
            else if (stateData.ContainsKey("ServerName"))
            {
                serverId = stateData["ServerName"];
            }

            if (serverId != null)
            {
                builder.Append("<dt>Server:</dt>");
                builder.Append($"<dd>{helper.ServerId(serverId)}</dd>");
            }

            if (stateData.ContainsKey("WorkerId"))
            {
                builder.Append("<dt>Worker:</dt>");
                builder.Append($"<dd>{helper.HtmlEncode(stateData["WorkerId"].Substring(0, 8))}</dd>");
            }
            else if (stateData.ContainsKey("WorkerNumber"))
            {
                builder.Append("<dt>Worker:</dt>");
                builder.Append($"<dd>#{helper.HtmlEncode(stateData["WorkerNumber"])}</dd>");
            }

            builder.Append("</dl>");

            return new NonEscapedString(builder.ToString());
        }

        private static NonEscapedString EnqueuedRenderer(HtmlHelper helper, IDictionary<string, string> stateData)
        {
            return new NonEscapedString(
                $"<dl class=\"dl-horizontal\"><dt>Queue:</dt><dd>{helper.QueueLabel(stateData["Queue"])}</dd></dl>");
        }

        private static NonEscapedString ScheduledRenderer(HtmlHelper helper, IDictionary<string, string> stateData)
        {
            var enqueueAt = JobHelper.DeserializeDateTime(stateData["EnqueueAt"]);

            return new NonEscapedString(
                $"<dl class=\"dl-horizontal\"><dt>Enqueue at:</dt><dd data-moment=\"{helper.HtmlEncode(JobHelper.ToTimestamp(enqueueAt).ToString(CultureInfo.InvariantCulture))}\">{helper.HtmlEncode(enqueueAt.ToString(CultureInfo.CurrentUICulture))}</dd></dl>");
        }

        private static NonEscapedString AwaitingRenderer(HtmlHelper helper, IDictionary<string, string> stateData)
        {
            var builder = new StringBuilder();

            builder.Append("<dl class=\"dl-horizontal\">");

            if (stateData.ContainsKey("ParentId"))
            {
                builder.Append($"<dt>Parent</dt><dd>{helper.JobIdLink(stateData["ParentId"])}</dd>");
            }

            if (stateData.ContainsKey("NextState"))
            {
                var nextState = SerializationHelper.Deserialize<IState>(stateData["NextState"], SerializationOption.TypedInternal);

                builder.Append($"<dt>Next State</dt><dd>{helper.StateLabel(nextState?.Name ?? "(no state)")}</dd>");
            }

            if (stateData.ContainsKey("Options"))
            {
                var optionsDescription = stateData["Options"];
                if (Enum.TryParse(optionsDescription, out JobContinuationOptions options))
                {
                    optionsDescription = options.ToString("G");
                }

                builder.Append($"<dt>Options</dt><dd><code>{helper.HtmlEncode(optionsDescription)}</code></dd>");
            }

            builder.Append("</dl>");

            return new NonEscapedString(builder.ToString());
        }
    }
}
