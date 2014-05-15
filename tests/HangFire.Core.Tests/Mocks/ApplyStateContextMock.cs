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
            ConnectionValue = new Mock<IStorageConnection>();
            StateContextValue = new StateContextMock();
            NewStateValue = new Mock<IState>().Object;
            OldStateValue = null;

            _context = new Lazy<ApplyStateContext>(
                () => new ApplyStateContext(
                    ConnectionValue.Object,
                    StateContextValue.Object,
                    NewStateValue,
                    OldStateValue));
        }

        public Mock<IStorageConnection> ConnectionValue { get; set; }
        public StateContextMock StateContextValue { get; set; }
        public IState NewStateValue { get; set; }
        public string OldStateValue { get; set; }

        public ApplyStateContext Object
        {
            get { return _context.Value; }
        }
    }
}
