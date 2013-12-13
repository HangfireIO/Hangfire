using System;
using System.Collections.Generic;
using HangFire.Common.States;
using HangFire.Filters;
using HangFire.States;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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

        public void OnStateApplied(StateApplyingContext context)
        {
            Assert.IsNotNull(context);

            _results.Add(String.Format("{0}::{1}", _name, "OnStateApplied"));
        }

        public void OnStateUnapplied(StateApplyingContext context)
        {
            Assert.IsNotNull(context);

            _results.Add(String.Format("{0}::{1}", _name, "OnStateUnapplied"));
        }
    }
}
