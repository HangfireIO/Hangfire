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

        public void OnStateApplied(
            JobDescriptor descriptor, JobState state, IRedisTransaction transaction)
        {
            Assert.IsNotNull(transaction);
            Assert.IsNotNull(descriptor);
            Assert.IsFalse(String.IsNullOrEmpty(descriptor.JobId));
            Assert.IsNotNull(state);

            _results.Add(String.Format("{0}::{1}", _name, "OnStateApplied"));
        }

        public void OnStateUnapplied(
            JobDescriptor descriptor, string stateName, IRedisTransaction transaction)
        {
            Assert.IsNotNull(transaction);
            Assert.IsNotNull(descriptor);
            Assert.IsFalse(String.IsNullOrEmpty(descriptor.JobId));
            Assert.IsFalse(String.IsNullOrEmpty(stateName));

            _results.Add(String.Format("{0}::{1}", _name, "OnStateUnapplied"));
        }
    }
}
