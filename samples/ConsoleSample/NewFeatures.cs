using System;
using Hangfire;

namespace ConsoleSample
{
    public static class NewFeatures
    {
        [AutomaticRetry(Attempts = 0, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
        public static bool TryExceptional(bool throwException)
        {
            if (throwException) throw new InvalidOperationException();
            return true;
        }

        public static void Continuation([FromResult] bool result)
        {
            Console.WriteLine("Success continuation, result: " + result);
        }

        public static void CatchExceptional([FromException] ExceptionInfo exception)
        {
            if (exception.Type.Contains("OperationCanceledException"))
            {
                Console.WriteLine("Failure continuation: Operation was canceled");
            }
            else
            {
                Console.WriteLine("Failure continuation: " + exception);
            }
        }

        public static void FinallyExceptional([FromException] ExceptionInfo exception)
        {
            if (exception == null)
            {
                Console.WriteLine("Finally clause, after success");
            }
            else
            {
                Console.WriteLine("Finally clause, after failure: " + exception);
            }
        }

        public static void FinallyExceptional2([FromResult] bool result, [FromException] ExceptionInfo exception)
        {
            if (exception == null)
            {
                Console.WriteLine("Finally clause 2, after success: " + result);
            }
            else
            {
                Console.WriteLine("Finally clause 2, after failure: " + exception);
            }
        }

        public static void Test(bool throwException)
        {
            var exceptionalId = BackgroundJob.Enqueue(() => TryExceptional(throwException));

            BackgroundJob.ContinueJobWith(exceptionalId, () => Continuation(default), JobContinuationOptions.OnlyOnSucceededState);
            BackgroundJob.ContinueJobWith(exceptionalId, () => CatchExceptional(default), JobContinuationOptions.OnlyOnDeletedState);
            BackgroundJob.ContinueJobWith(exceptionalId, () => FinallyExceptional(default), JobContinuationOptions.OnlyOnSucceededState | JobContinuationOptions.OnlyOnDeletedState);
            BackgroundJob.ContinueJobWith(exceptionalId, () => FinallyExceptional2(default, default), JobContinuationOptions.OnAnyFinishedState);
        }
    }
}