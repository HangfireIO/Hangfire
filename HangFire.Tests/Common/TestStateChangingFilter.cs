using System;
using System.Collections.Generic;
using HangFire.Filters;
using HangFire.States;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ServiceStack.Redis;

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

        public JobState OnStateChanging(
            JobDescriptor descriptor, JobState state, IRedisClient redis)
        {
            Assert.IsNotNull(redis);
            Assert.IsNotNull(descriptor);
            Assert.IsFalse(String.IsNullOrEmpty(descriptor.JobId));
            Assert.IsNotNull(state);

            _results.Add(_name);

            return _changeState ?? state;
        }
    }
}
