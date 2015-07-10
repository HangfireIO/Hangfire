using System;
using System.Threading;
using System.Threading.Tasks;
using Hangfire.Annotations;
using Hangfire.Logging;

namespace Hangfire.Server
{
    internal static class ServerComponentExtensions
    {
        public static Task CreateTask([NotNull] this IServerComponent component, CancellationToken cancellationToken)
        {
            if (component == null) throw new ArgumentNullException("component");

            return Task.Factory.StartNew(
                () => RunComponent(component, cancellationToken),
                cancellationToken,
                TaskCreationOptions.AttachedToParent | TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
        }

        private static void RunComponent(IServerComponent component, CancellationToken cancellationToken)
        {
            // Long-running tasks are based on custom threads (not threadpool ones) as in 
            // .NET Framework 4.5, so we can try to set custom thread name to simplify the
            // debugging experience.
            TrySetThreadName(component.ToString());

            // LogProvider.GetLogger does not throw any exception, that is why we are not
            // using the `try` statement here. It does not return `null` value as well.
            var logger = LogProvider.GetLogger(component.ToString());
            logger.DebugFormat("Server component '{0}' started.", component);

            try
            {
                component.Execute(cancellationToken);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                logger.FatalException(
                    String.Format(
                        "Fatal error occurred during execution of '{0}' component. It will be stopped. See the exception for details.",
                        component),
                    ex);
            }

            logger.DebugFormat("Server component '{0}' stopped.", component);
        }

        private static void TrySetThreadName(string name)
        {
            try
            {
                Thread.CurrentThread.Name = name;
            }
            catch (InvalidOperationException)
            {
            }
        }
    }
}