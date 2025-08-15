#if NETSTANDARD2_0
#nullable enable

using System;
using System.Diagnostics;
using Hangfire.Client;
using Hangfire.Server;
using Hangfire.States;
using Hangfire.Storage;

namespace Hangfire
{
    public sealed class DiagnosticsActivityFilter : IClientFilter, IServerFilter, IDisposable
    {
        public const string DefaultListenerName = "Hangfire";

        private static readonly ActivitySource DefaultActivitySource = new ActivitySource(DefaultListenerName);

        private const string ActivityItemsKeyName = "Diagnostics.Activity";

        private const string ExceptionEventName = "exception";
        private const string ExceptionMessageTag = "exception.message";
        private const string ExceptionStackTraceTag = "exception.stacktrace";
        private const string ExceptionTypeTag = "exception.type";

        private const string MessagingDestinationNameTag = "messaging.destination.name";
        private const string MessagingMessageId = "messaging.message.id";
        private const string MessagingOperationName = "messaging.operation.name";
        private const string MessagingOperationType = "messaging.operation.type";

        private const string TraceParentParameterName = "traceparent";
        private const string TraceStateParameterName = "tracestate";

        private readonly ActivitySource _activitySource;

        public DiagnosticsActivityFilter()
        {
            _activitySource = DefaultActivitySource;
        }

        public DiagnosticsActivityFilter(string? activitySourceName)
        {
            var name = activitySourceName ?? throw new ArgumentNullException(nameof(activitySourceName));
            _activitySource = new ActivitySource(name);
        }

        public void OnCreating(CreatingContext context)
        {
            var activity = _activitySource.StartActivity(
                $"create_job {context.Job.Type.Name}.{context.Job.Method.Name}",
                ActivityKind.Producer);

            if (activity != null)
            {
                activity.SetTag(MessagingOperationName, "create_job");
                activity.SetTag(MessagingDestinationNameTag, $"{context.Job.Type.Name}.{context.Job.Method.Name}");
                activity.SetTag(MessagingOperationType, "create");

                activity.SetTag("job.type", context.Job.Type.FullName);
                activity.SetTag("job.method", context.Job.Method.Name);
                activity.SetTag("job.state", context.InitialState?.Name);
                activity.SetTag("job.storage", context.Storage.ToString());

                context.SetJobParameter(TraceParentParameterName, activity.Id);
                context.SetJobParameter(TraceStateParameterName, activity.TraceStateString);

                context.Items[ActivityItemsKeyName] = activity;
            }
        }

        public void OnCreated(CreatedContext context)
        {
            if (context.Items.TryGetValue(ActivityItemsKeyName, out var item) &&
                item is Activity activity)
            {
                if (context.Exception == null)
                {
                    // NOTE: Need library 6.0 for SetStatus(ActivityStatusCode.Ok) (use tags instead)
                    activity.SetTag("otel.status_code", "OK");

                    activity.SetTag(MessagingMessageId, context.BackgroundJob.Id);
                    activity.SetTag("job.id", context.BackgroundJob.Id);
                }
                else
                {
                    // NOTE: Library 9.0 has AddException (manually add event instead)
                    var exceptionTags = new ActivityTagsCollection
                    {
                        { ExceptionMessageTag, context.Exception.Message },
                        { ExceptionTypeTag, context.Exception.GetType().ToString() },
                        { ExceptionStackTraceTag, context.Exception.ToString() }
                    };
                    activity.AddEvent(new ActivityEvent(ExceptionEventName, tags: exceptionTags));

                    // NOTE: Need library 6.0 for SetStatus(ActivityStatusCode.Error, "Exception") (use tags instead)
                    activity.SetTag("otel.status_code", "ERROR");
                    activity.SetTag("otel.status_description", "Exception");
                }

                activity.Dispose();
            }
        }

        public void OnPerforming(PerformingContext context)
        {
            var parentId = context.GetJobParameter<string>(TraceParentParameterName);
            var parentState = context.GetJobParameter<string>(TraceStateParameterName);
            ActivityContext.TryParse(parentId, parentState, out var parentCtx);

            var activity = _activitySource.StartActivity(
                $"perform_job {context.BackgroundJob.Job.Type.Name}.{context.BackgroundJob.Job.Method.Name}",
                ActivityKind.Consumer,
                parentCtx);

            if (activity != null)
            {
                activity.SetTag(MessagingOperationName, "perform_job");
                activity.SetTag(MessagingDestinationNameTag, $"{context.BackgroundJob.Job.Type.Name}.{context.BackgroundJob.Job.Method.Name}");
                activity.SetTag(MessagingOperationType, "process");
                activity.SetTag(MessagingMessageId, context.BackgroundJob.Id);

                context.Items[ActivityItemsKeyName] = activity;
            }
        }

        public void OnPerformed(PerformedContext context)
        {
            if (context.Items.TryGetValue(ActivityItemsKeyName, out var item) && item is Activity activity)
            {
                if (context.Exception == null)
                {
                    // NOTE: Need library 6.0 for SetStatus(ActivityStatusCode.Ok) (use tags instead)
                    activity.SetTag("otel.status_code", "OK");
                }
                else
                {
                    // NOTE: Library 9.0 has AddException (manually add event instead)
                    var exceptionTags = new ActivityTagsCollection
                    {
                        { ExceptionMessageTag, context.Exception.Message },
                        { ExceptionTypeTag, context.Exception.GetType().ToString() },
                        { ExceptionStackTraceTag, context.Exception.ToString() }
                    };
                    activity.AddEvent(new ActivityEvent(ExceptionEventName, tags: exceptionTags));

                    // NOTE: Need library 6.0 for SetStatus(ActivityStatusCode.Error, "Exception") (use tags instead)
                    activity.SetTag("otel.status_code", "ERROR");
                    activity.SetTag("otel.status_description", "Exception");
                }

                activity.Dispose();
            }
        }

        public void Dispose()
        {
            _activitySource?.Dispose();
        }
    }
}
#endif
