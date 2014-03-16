using System;
using System.Collections.Generic;
using HangFire.Common.States;
using HangFire.Filters;
using HangFire.States;
using HangFire.Storage;
using Xunit;

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
            StateApplyingContext context, IWriteOnlyTransaction transaction)
        {
            Assert.NotNull(context);
            Assert.NotNull(transaction);

            _results.Add(String.Format("{0}::{1}", _name, "OnStateApplied"));
        }

        public void OnStateUnapplied(
            StateApplyingContext context, IWriteOnlyTransaction transaction)
        {
            Assert.NotNull(context);
            Assert.NotNull(transaction);

            _results.Add(String.Format("{0}::{1}", _name, "OnStateUnapplied"));
        }
    }
}
