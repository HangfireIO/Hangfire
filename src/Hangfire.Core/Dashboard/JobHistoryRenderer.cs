// This file is part of Hangfire.
// Copyright © 2013-2014 Sergey Odinokov.
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
using System.Text;
using Hangfire.Common;
using Hangfire.States;

namespace Hangfire.Dashboard
{
    internal static class JobHistoryRenderer
    {
        private static readonly IDictionary<string, Func<IDictionary<string, string>, NonEscapedString>> 
            Renderers = new Dictionary<string, Func<IDictionary<string, string>, NonEscapedString>>();
        public static readonly IDictionary<string, string> BackgroundStateColors
            = new Dictionary<string, string>();
        public static readonly IDictionary<string, string> ForegroundStateColors
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

            BackgroundStateColors.Add(EnqueuedState.StateName, "#F5F5F5");
            BackgroundStateColors.Add(SucceededState.StateName, "#EDF7ED");
            BackgroundStateColors.Add(FailedState.StateName, "#FAEBEA");
            BackgroundStateColors.Add(ProcessingState.StateName, "#FCEFDC");
            BackgroundStateColors.Add(ScheduledState.StateName, "#E0F3F8");
            BackgroundStateColors.Add(DeletedState.StateName, "#ddd");

            ForegroundStateColors.Add(EnqueuedState.StateName, "#999");
            ForegroundStateColors.Add(SucceededState.StateName, "#5cb85c");
            ForegroundStateColors.Add(FailedState.StateName, "#d9534f");
            ForegroundStateColors.Add(ProcessingState.StateName, "#f0ad4e");
            ForegroundStateColors.Add(ScheduledState.StateName, "#5bc0de");
            ForegroundStateColors.Add(DeletedState.StateName, "#777");
        }

        public static void Register(string state, Func<IDictionary<string, string>, NonEscapedString> renderer)
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

        public static NonEscapedString Render(string state, IDictionary<string, string> properties)
        {
            return Renderers[state](properties);
        }

        public static NonEscapedString NullRenderer(IDictionary<string, string> properties)
        {
            return null;
        }

        public static NonEscapedString SucceededRenderer(IDictionary<string, string> stateData)
        {
            var builder = new StringBuilder();
            builder.Append("<dl class=\"dl-horizontal\">");

            var itemsAdded = false;

            if (stateData.ContainsKey("Latency"))
            {
                var latency = TimeSpan.FromMilliseconds(int.Parse(stateData["Latency"]));
                builder.AppendFormat("<dt>Latency:</dt><dd>{0}</dd>", HtmlHelper.ToHumanDuration(latency, false));

                itemsAdded = true;
            }

            if (stateData.ContainsKey("PerformanceDuration"))
            {
                var duration = TimeSpan.FromMilliseconds(int.Parse(stateData["PerformanceDuration"]));
                builder.AppendFormat("<dt>Duration:</dt><dd>{0}</dd>", HtmlHelper.ToHumanDuration(duration, false));

                itemsAdded = true;
            }


            if (stateData.ContainsKey("Result"))
            {
                var result = stateData["Result"];
                builder.AppendFormat("<dt>Result:</dt><dd>{0}</dd>", System.Net.WebUtility.HtmlEncode(result));

                itemsAdded = true;
            }

            builder.Append("</dl>");

            if (!itemsAdded) return null;

            return new NonEscapedString(builder.ToString());
        }

        private static NonEscapedString FailedRenderer(IDictionary<string, string> stateData)
        {
            var stackTrace = HtmlHelper.MarkupStackTrace(stateData["ExceptionDetails"]).ToString();
            return new NonEscapedString(String.Format(
                "<h4 class=\"exception-type\">{0}</h4><p>{1}</p>{2}",
                stateData["ExceptionType"],
                stateData["ExceptionMessage"],
                stackTrace != null ? "<pre class=\"stack-trace\">" + stackTrace + "</pre>" : null));
        }

        private static NonEscapedString ProcessingRenderer(IDictionary<string, string> stateData)
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
                builder.AppendFormat(
                    "<dd><span class=\"label label-default\">{0}</span></dd>",
                    serverId.ToUpperInvariant());
            }

            if (stateData.ContainsKey("WorkerNumber"))
            {
                builder.Append("<dt>Worker:</dt>");
                builder.AppendFormat("<dd>#{0}</dd>", stateData["WorkerNumber"]);
            }

            builder.Append("</dl>");

            return new NonEscapedString(builder.ToString());
        }

        private static NonEscapedString EnqueuedRenderer(IDictionary<string, string> stateData)
        {
            return new NonEscapedString(String.Format(
                "<dl class=\"dl-horizontal\"><dt>Queue:</dt><dd><span class=\"label label-queue label-primary\">{0}</span></dd></dl>",
                stateData["Queue"]));
        }

        private static NonEscapedString ScheduledRenderer(IDictionary<string, string> stateData)
        {
            var enqueueAt = JobHelper.DeserializeDateTime(stateData["EnqueueAt"]);

            return new NonEscapedString(String.Format(
                "<dl class=\"dl-horizontal\"><dt>Enqueue at:</dt><dd data-moment=\"{0}\">{1}</dd></dl>",
                JobHelper.ToTimestamp(enqueueAt),
                enqueueAt));
        }
    }
}
