using System;
using Hangfire.States;

namespace Hangfire.Core.Tests
{
    class ElectStateContextMock
    {
        private readonly Lazy<ElectStateContext> _context;

        public ElectStateContextMock()
        {
            ApplyContext = new ApplyStateContextMock();

            _context = new Lazy<ElectStateContext>(
                () => new ElectStateContext(ApplyContext.Object));
        }

        public ApplyStateContextMock ApplyContext { get; set; }

        public ElectStateContext Object => _context.Value;
    }
}
