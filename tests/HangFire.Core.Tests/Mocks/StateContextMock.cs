using System;
using HangFire.Common;
using HangFire.States;

namespace HangFire.Core.Tests
{
    public class StateContextMock
    {
        private readonly Lazy<StateContext> _context;

        public StateContextMock()
        {
            JobIdValue = "job-id";
            JobValue = Job.FromExpression(() => Method());

            _context = new Lazy<StateContext>(
                () => new StateContext(JobIdValue, JobValue));
        }

        public string JobIdValue { get; set; }
        public Job JobValue { get; set; }

        public StateContext Object
        {
            get { return _context.Value; }
        }

        public static void Method()
        {
        }
    }
}
