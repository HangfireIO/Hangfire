using System;
using System.Reflection;
using Moq.Sequences;
using Xunit;
using Xunit.Sdk;

namespace Hangfire.Core.Tests
{
    public class SequenceAttribute : BeforeAfterTestAttribute
    {
        private IDisposable _sequence;

        public override void Before(MethodInfo methodUnderTest)
        {
            _sequence = Sequence.Create();
        }

        public override void After(MethodInfo methodUnderTest)
        {
            _sequence.Dispose();
        }
    }
}
