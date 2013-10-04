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
        public static readonly IDictionary<string, string> StateColors
            = new Dictionary<string, string>();

        static JobHistoryRenderer()
        {
            Register(EnqueuedState.Name, NullRenderer);
            Register(SucceededState.Name, NullRenderer);
            Register(FailedState.Name, FailedRenderer);
            Register(ProcessingState.Name, ProcessingRenderer);
            Register(ScheduledState.Name, ScheduledRenderer);

            StateColors.Add(EnqueuedState.Name, "#F5F5F5");
            StateColors.Add(SucceededState.Name, "#EDF7ED");
            StateColors.Add(FailedState.Name, "#FAEBEA");
            StateColors.Add(ProcessingState.Name, "#FCEFDC");
            StateColors.Add(ScheduledState.Name, "#E0F3F8");
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

        private static IHtmlString ScheduledRenderer(IDictionary<string, string> properties)
        {
            return new HtmlString(String.Format(
                "<dl class=\"dl-horizontal\"><dt>Scheduled queue:</dt><dd><span class=\"label label-primary\">{0}</span></dd></dl>",
                properties["ScheduledQueue"]));
        }
    }
}
