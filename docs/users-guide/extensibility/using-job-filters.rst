Using job filters
==================

All processes are implemented with Chain-of-responsibility pattern and can be intercepted like with ASP.NET MVC Action Filters.

**Define the filter**

.. code-block:: c#

    public class LogEverythingAttribute : JobFilterAttribute,
            IClientFilter, IServerFilter, IElectStateFilter, IApplyStateFilter
    {
        private static readonly ILog Logger = LogManager.GetCurrentClassLogger();

        public void OnCreating(CreatingContext filterContext)
        {
            Logger.InfoFormat(
                "Creating a job based on method `{0}`...", 
                filterContext.Job.MethodData.MethodInfo.Name);
        }

        public void OnCreated(CreatedContext filterContext)
        {
            Logger.InfoFormat(
                "Job that is based on method `{0}` has been created with id `{1}`", 
                filterContext.Job.MethodData.MethodInfo.Name, 
                filterContext.JobId);
        }

        public void OnPerforming(PerformingContext filterContext)
        {
            Logger.InfoFormat(
                "Starting to perform job `{0}`",
                filterContext.JobId);
        }

        public void OnPerformed(PerformedContext filterContext)
        {
            Logger.InfoFormat(
                "Job `{0}` has been performed",
                filterContext.JobId);
        }

        public void OnStateElection(ElectStateContext context)
        {
            var failedState = context.CandidateState as FailedState;
            if (failedState != null)
            {
                Logger.WarnFormat(
                    "Job `{0}` has been failed due to exception `{1}` but will be retried automatically until retry attempts exceeded",
                    context.JobId,
                    failedState.Exception);
            }
        }

        public void OnStateApplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
        {
            Logger.InfoFormat(
                "Job `{0}` state was changed from `{1}` to `{2}`",
                context.JobId,
                context.OldStateName,
                context.NewState.Name);
        }

        public void OnStateUnapplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
        {
            Logger.InfoFormat(
                "Job `{0}` state `{1}` was unapplied.",
                context.JobId,
                context.OldStateName);
        }
    }

**Apply it**

Like ASP.NET filters, you can apply filters on method, class and globally:

.. code-block:: c#

    [LogEverything]
    public class EmailService
    {
        [LogEverything]
        public static void Send() { }
    }

    GlobalJobFilters.Filters.Add(new LogEverythingAttribute());
