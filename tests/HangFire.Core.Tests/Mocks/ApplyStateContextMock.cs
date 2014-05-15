using System;
using HangFire.States;
using HangFire.Storage;
using Moq;

namespace HangFire.Core.Tests
{
    public class ApplyStateContextMock
    {
        private readonly Lazy<ApplyStateContext> _context;

        public ApplyStateContextMock()
        {
            StateContextValue = new StateContextMock();
            NewStateValue = new Mock<IState>().Object;
            OldStateValue = null;

            _context = new Lazy<ApplyStateContext>(
                () => new ApplyStateContext(
                    StateContextValue.Object,
                    NewStateValue,
                    OldStateValue));
        }

        public StateContextMock StateContextValue { get; set; }
        public IState NewStateValue { get; set; }
        public string OldStateValue { get; set; }

        public ApplyStateContext Object
        {
            get { return _context.Value; }
        }
    }
}
