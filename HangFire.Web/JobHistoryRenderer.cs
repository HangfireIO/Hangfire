using System;
using System.Collections.Generic;
using System.Web;
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
            Register(EnqueuedState.Name, NullRenderer);
            Register(SucceededState.Name, NullRenderer);
            Register(FailedState.Name, FailedRenderer);
            Register(ProcessingState.Name, ProcessingRenderer);
            Register(EnqueuedState.Name, EnqueuedRenderer);
            Register(ScheduledState.Name, ScheduledRenderer);

            BackgroundStateColors.Add(EnqueuedState.Name, "#F5F5F5");
            BackgroundStateColors.Add(SucceededState.Name, "#EDF7ED");
            BackgroundStateColors.Add(FailedState.Name, "#FAEBEA");
            BackgroundStateColors.Add(ProcessingState.Name, "#FCEFDC");
            BackgroundStateColors.Add(ScheduledState.Name, "#E0F3F8");

            ForegroundStateColors.Add(EnqueuedState.Name, "#999");
            ForegroundStateColors.Add(SucceededState.Name, "#5cb85c");
            ForegroundStateColors.Add(FailedState.Name, "#d9534f");
            ForegroundStateColors.Add(ProcessingState.Name, "#f0ad4e");
            ForegroundStateColors.Add(ScheduledState.Name, "#5bc0de");
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

        private static IHtmlString FailedRenderer(IDictionary<string, string> properties)
        {
            var stackTrace = HtmlHelper.MarkupStackTrace(properties["ExceptionDetails"]).ToHtmlString();
            return new HtmlString(String.Format(
                "<h4 class=\"exception-type\">{0}</h4><p>{1}</p>{2}",
                properties["ExceptionType"],
                properties["ExceptionMessage"],
                stackTrace != null ? "<pre class=\"stack-trace\">" + stackTrace + "</pre>" : null));
        }

        private static IHtmlString ProcessingRenderer(IDictionary<string, string> properties)
        {
            return new HtmlString(String.Format(
                "<dl class=\"dl-horizontal\"><dt>Server:</dt><dd><span class=\"label label-default\">{0}</span></dd></dl>", properties["ServerName"]));
        }

        private static IHtmlString EnqueuedRenderer(IDictionary<string, string> properties)
        {
            return new HtmlString(String.Format(
                "<dl class=\"dl-horizontal\"><dt>Queue:</dt><dd><span class=\"label label-primary\">{0}</span></dd></dl>",
                properties["Queue"]));
        }

        private static IHtmlString ScheduledRenderer(IDictionary<string, string> properties)
        {
            return new HtmlString(String.Format(
                "<dl class=\"dl-horizontal\"><dt>Enqueue at:</dt><dd data-moment=\"{0}\">{1}</dd></dl>",
                properties["EnqueueAt"],
                JobHelper.FromStringTimestamp(properties["EnqueueAt"])));
        }
    }
}
