using System.Collections.Generic;
using HangFire.Filters;
using HangFire.States;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HangFire.Tests
{
    public class TestStateChangingFilter : IStateChangingFilter
    {
        private readonly string _name;
        private readonly IList<string> _results;
        private readonly JobState _changeState;

        public TestStateChangingFilter(string name, IList<string> results, JobState changeState = null)
        {
            _name = name;
            _results = results;
            _changeState = changeState;
        }

        public void OnStateChanging(StateChangingContext context)
        {
            Assert.IsNotNull(context);

            _results.Add(_name);

            if (_changeState != null)
            {
                context.CandidateState = _changeState;
            }
        }
    }
}
