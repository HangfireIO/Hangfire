using System;
using System.Collections.Generic;
using HangFire.Filters;

namespace HangFire.Tests
{
    public class TestClientFilter : IClientFilter
    {
        private readonly string _name;
        private readonly ICollection<string> _results;
        private readonly bool _throwException;
        private readonly bool _cancelsTheCreation;
        private readonly bool _handlesException;

        public TestClientFilter(
            string name, 
            ICollection<string> results, 
            bool throwException = false, 
            bool cancelsTheCreation = false,
            bool handlesException = false)
        {
            _name = name;
            _results = results;
            _throwException = throwException;
            _cancelsTheCreation = cancelsTheCreation;
            _handlesException = handlesException;
        }

        public void OnCreating(CreatingContext filterContext)
        {
            _results.Add(String.Format("{0}::{1}", _name, "OnCreating"));

            if (_cancelsTheCreation)
            {
                filterContext.Canceled = true;
            }
            
            if (_throwException)
            {
                throw new Exception();
            } 
        }

        public void OnCreated(CreatedContext filterContext)
        {
            _results.Add(String.Format("{0}::{1}", _name, "OnCreated") 
                + (filterContext.Canceled ? " (with the canceled flag set)" : null));

            if (_handlesException)
            {
                filterContext.ExceptionHandled = true;
            }
        }
    }
}
