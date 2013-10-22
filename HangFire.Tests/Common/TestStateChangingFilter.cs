using System;
using System.Collections.Generic;
using HangFire.States;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ServiceStack.Redis;

namespace HangFire.Tests
{
    public class TestStateChangingFilter : IStateChangedFilter
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

        public JobState OnStateChanged(IRedisClient redis, string jobId, JobState state)
        {
            Assert.IsNotNull(redis);
            Assert.IsFalse(String.IsNullOrEmpty(jobId));
            Assert.IsNotNull(state);

            _results.Add(_name);

            return _changeState ?? state;
        }
    }
}
