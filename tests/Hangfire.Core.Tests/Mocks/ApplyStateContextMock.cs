using System;
using System.Collections.Generic;
using System.Linq;
using Hangfire.States;
using Moq;

namespace Hangfire.Core.Tests
{
    public class ApplyStateContextMock
    {
        private readonly Lazy<ApplyStateContext> _context;

        public ApplyStateContextMock()
        {
            StateContextValue = new StateContextMock();
            NewStateValue = new Mock<IState>().Object;
            OldStateValue = null;
            TraversedStatesValue = Enumerable.Empty<IState>();

            _context = new Lazy<ApplyStateContext>(
                () => new ApplyStateContext(
                    StateContextValue.Object,
                    NewStateValue,
                    OldStateValue,
                    TraversedStatesValue));
        }

        public StateContextMock StateContextValue { get; set; }
        public IState NewStateValue { get; set; }
        public string OldStateValue { get; set; }
        public IEnumerable<IState> TraversedStatesValue { get; set; } 

        public ApplyStateContext Object
        {
            get { return _context.Value; }
        }
    }
}
