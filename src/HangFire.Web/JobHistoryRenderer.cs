// This file is part of HangFire.
// Copyright © 2013-2014 Sergey Odinokov.
// 
// HangFire is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as 
// published by the Free Software Foundation, either version 3 
// of the License, or any later version.
// 
// HangFire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public 
// License along with HangFire. If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Web;
using HangFire.Common;
using HangFire.States;

namespace HangFire.Web
{
    internal static class JobHistoryRenderer
    {
        private static readonly IDictionary<string, Func<IDictionary<string, string>, IHtmlString>> 
            Renderers = new Dictionary<string, Func<IDictionary<string, string>, IHtmlString>>();
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

        public static void Register(string state, Func<IDictionary<string, string>, IHtmlString> renderer)
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

        public static IHtmlString Render(string state, IDictionary<string, string> properties)
        {
            return Renderers[state](properties);
        }

        public static IHtmlString NullRenderer(IDictionary<string, string> properties)
        {
            return null;
        }

        public static IHtmlString SucceededRenderer(IDictionary<string, string> stateData)
        {
            if (stateData.ContainsKey("PerformanceDuration"))
            {
                var duration = TimeSpan.FromMilliseconds(int.Parse(stateData["PerformanceDuration"]));

                return new HtmlString(String.Format(
                    "<dl class=\"dl-horizontal\"><dt>Duration:</dt><dd>{0}</dd></dl>",
                    HtmlHelper.ToHumanDuration(duration, false)));
            }

            return null;
        }

        private static IHtmlString FailedRenderer(IDictionary<string, string> stateData)
        {
            var stackTrace = HtmlHelper.MarkupStackTrace(stateData["ExceptionDetails"]).ToHtmlString();
            return new HtmlString(String.Format(
                "<h4 class=\"exception-type\">{0}</h4><p>{1}</p>{2}",
                stateData["ExceptionType"],
                stateData["ExceptionMessage"],
                stackTrace != null ? "<pre class=\"stack-trace\">" + stackTrace + "</pre>" : null));
        }

        private static IHtmlString ProcessingRenderer(IDictionary<string, string> stateData)
        {
            return new HtmlString(String.Format(
                "<dl class=\"dl-horizontal\"><dt>Server:</dt><dd><span class=\"label label-default\">{0}</span></dd></dl>", stateData["ServerName"].ToUpperInvariant()));
        }

        private static IHtmlString EnqueuedRenderer(IDictionary<string, string> stateData)
        {
            return new HtmlString(String.Format(
                "<dl class=\"dl-horizontal\"><dt>Queue:</dt><dd><span class=\"label label-queue label-primary\">{0}</span></dd></dl>",
                stateData["Queue"]));
        }

        private static IHtmlString ScheduledRenderer(IDictionary<string, string> stateData)
        {
            return new HtmlString(String.Format(
                "<dl class=\"dl-horizontal\"><dt>Enqueue at:</dt><dd data-moment=\"{0}\">{1}</dd></dl>",
                stateData["EnqueueAt"],
                JobHelper.FromStringTimestamp(stateData["EnqueueAt"])));
        }
    }
}
