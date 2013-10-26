using System;
using System.Collections.Generic;
using HangFire.Filters;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HangFire.Tests
{
    public class TestFilter : IClientFilter, IServerFilter
    {
        private readonly string _name;
        private readonly ICollection<string> _results;
        private readonly bool _throwException;
        private readonly bool _cancelsTheCreation;
        private readonly bool _handlesException;
        private readonly IDictionary<string, string> _setOnCreatingParameters;
        private readonly IDictionary<string, string> _readParameters;
        private readonly IDictionary<string, string> _setOnCreatedParameters;

        public TestFilter(
            string name, 
            ICollection<string> results, 
            bool throwException = false, 
            bool cancelsTheCreation = false,
            bool handlesException = false,
            IDictionary<string, string> setOnCreatingParameters = null,
            IDictionary<string, string> readParameters = null,
            IDictionary<string, string> setOnCreatedParameters = null)
        {
            _name = name;
            _results = results;
            _throwException = throwException;
            _cancelsTheCreation = cancelsTheCreation;
            _handlesException = handlesException;
            _setOnCreatingParameters = setOnCreatingParameters;
            _readParameters = readParameters;
            _setOnCreatedParameters = setOnCreatedParameters;
        }

        public void OnCreating(CreatingContext filterContext)
        {
            Assert.IsNotNull(filterContext);
            Assert.IsNotNull(filterContext.Items);
            Assert.IsNotNull(filterContext.Redis);
            Assert.IsNotNull(filterContext.JobDescriptor);
            Assert.IsNotNull(filterContext.JobDescriptor.JobId);
            Assert.IsNotNull(filterContext.JobDescriptor.Type);
            Assert.IsNotNull(filterContext.JobDescriptor.State);

            if (_cancelsTheCreation)
            {
                filterContext.Canceled = true;
            }

            _results.Add(String.Format("{0}::{1}", _name, "OnCreating"));

            if (_setOnCreatingParameters != null)
            {
                foreach (var parameter in _setOnCreatingParameters)
                {
                    filterContext.JobDescriptor.SetParameter(parameter.Key, parameter.Value);
                }
            }

            if (_readParameters != null)
            {
                foreach (var parameter in _readParameters)
                {
                    Assert.AreEqual(
                        parameter.Value, 
                        filterContext.JobDescriptor.GetParameter<string>(parameter.Key));
                }
            }
            
            if (_throwException)
            {
                throw new Exception();
            } 
        }

        public void OnCreated(CreatedContext filterContext)
        {
            Assert.IsNotNull(filterContext);
            Assert.IsNotNull(filterContext.Redis);
            Assert.IsNotNull(filterContext.Items);
            Assert.IsNotNull(filterContext.JobDescriptor);

            _results.Add(String.Format("{0}::{1}", _name, "OnCreated") 
                + (filterContext.Canceled ? " (with the canceled flag set)" : null));

            if (_setOnCreatedParameters != null)
            {
                foreach (var parameter in _setOnCreatedParameters)
                {
                    filterContext.JobDescriptor.SetParameter(parameter.Key, parameter.Value);
                }
            }

            if (_readParameters != null)
            {
                foreach (var parameter in _readParameters)
                {
                    Assert.AreEqual(
                        parameter.Value,
                        filterContext.JobDescriptor.GetParameter<string>(parameter.Key));
                }
            }

            if (_handlesException)
            {
                filterContext.ExceptionHandled = true;
            }
        }

        public void OnPerforming(PerformingContext filterContext)
        {
            Assert.IsNotNull(filterContext);
            Assert.IsNotNull(filterContext.JobDescriptor);

            if (_cancelsTheCreation)
            {
                filterContext.Canceled = true;
            }

            _results.Add(String.Format("{0}::{1}", _name, "OnPerforming"));
            
            if (_throwException)
            {
                throw new Exception();
            } 
        }

        public void OnPerformed(PerformedContext filterContext)
        {
            Assert.IsNotNull(filterContext);
            Assert.IsNotNull(filterContext.JobDescriptor);

            _results.Add(String.Format("{0}::{1}", _name, "OnPerformed")
                + (filterContext.Canceled ? " (with the canceled flag set)" : null));

            if (_handlesException)
            {
                filterContext.ExceptionHandled = true;
            }
        }
    }
}
