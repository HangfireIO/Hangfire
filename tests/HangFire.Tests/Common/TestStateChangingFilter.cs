using System.Collections.Generic;
using HangFire.Common.States;
using HangFire.Filters;
using HangFire.States;
using Xunit;

namespace HangFire.Tests
{
    public class TestStateChangingFilter : IStateChangingFilter
    {
        private readonly string _name;
        private readonly IList<string> _results;
        private readonly State _changeState;

        public TestStateChangingFilter(string name, IList<string> results, State changeState = null)
        {
            _name = name;
            _results = results;
            _changeState = changeState;
        }

        public void OnStateChanging(StateChangingContext context)
        {
            Assert.NotNull(context);

            _results.Add(_name);

            if (_changeState != null)
            {
                context.CandidateState = _changeState;
            }
        }
    }
}
