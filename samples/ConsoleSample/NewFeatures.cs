using System;
using Hangfire;

namespace ConsoleSample
{
    public static class NewFeatures
    {
        [AutomaticRetry(Attempts = 1, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
        public static void TryExceptional()
        {
            throw new InvalidOperationException();
        }

        public static void Continuation([FromResult] object result)
        {
            // Background job execution succeeded, and optional result was returned.
        }

        public static void CatchExceptional([FromException] ExceptionInfo exception)
        {
            if (exception.Type != typeof(OperationCanceledException))
            {
                // Background method threw an exception.
            }
            else
            {
                // Execution was canceled – someone clicked the "Delete" button, etc.
            }
        }

        public static void FinallyExceptional([FromException] ExceptionInfo exception)
        {
            if (exception == null)
            {
                // Succeeded
            }
            else
            {
                // An exception has been thrown, or execution was canceled.
            }
        }

        public static void FinallyExceptional2([FromResult] object result, [FromException] ExceptionInfo exception)
        {
            if (exception == null)
            {
                // Succeeded, result can be used if antecedent method returns something.
            }
            else
            {
                // An exception has been thrown, or execution was canceled.
            }
        }

        public static void Test()
        {
            var exceptionalId = BackgroundJob.Enqueue(() => TryExceptional());

            BackgroundJob.ContinueJobWith(exceptionalId, () => Continuation(null), JobContinuationOptions.OnlyOnSucceededState);
            BackgroundJob.ContinueJobWith(exceptionalId, () => CatchExceptional(null), JobContinuationOptions.OnlyOnDeletedState);
            BackgroundJob.ContinueJobWith(exceptionalId, () => FinallyExceptional(null), JobContinuationOptions.OnAnyFinishedState);
        }
    }
}