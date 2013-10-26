using System;
using System.Collections.Generic;
using HangFire.Filters;
using HangFire.States;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ServiceStack.Redis;

namespace HangFire.Tests
{
    public class TestStateChangedFilter : IStateChangedFilter
    {
        private readonly string _name;
        private readonly IList<string> _results;

        public TestStateChangedFilter(string name, IList<string> results)
        {
            _name = name;
            _results = results;
        }

        public void OnStateApplied(IRedisTransaction transaction, string jobId, JobState state)
        {
            Assert.IsNotNull(transaction);
            Assert.IsFalse(String.IsNullOrEmpty(jobId));
            Assert.IsNotNull(state);

            _results.Add(String.Format("{0}::{1}", _name, "OnStateApplied"));
        }

        public void OnStateUnapplied(IRedisTransaction transaction, string jobId, string state)
        {
            Assert.IsNotNull(transaction);
            Assert.IsFalse(String.IsNullOrEmpty(jobId));
            Assert.IsFalse(String.IsNullOrEmpty(state));

            _results.Add(String.Format("{0}::{1}", _name, "OnStateUnapplied"));
        }
    }
}
