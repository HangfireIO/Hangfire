using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace HangFire
{
    internal class JobInvoker
    {
        private readonly IEnumerable<IServerFilter> _filters;

        public JobInvoker(IEnumerable<IServerFilter> filters)
        {
            if (filters == null)
            {
                throw new ArgumentNullException("filters");
            }

            _filters = filters;
        }

        public void InvokeJob(HangFireJob instance, Dictionary<string, string> args)
        {
            if (instance == null) throw new ArgumentNullException("instance");
            if (args == null) throw new ArgumentNullException("args");

            foreach (var arg in args)
            {
                var propertyInfo = instance.GetType().GetProperty(arg.Key);
                if (propertyInfo != null)
                {
                    var converter = TypeDescriptor.GetConverter(propertyInfo.PropertyType);

                    // TODO: handle deserialization exception and display it in a friendly way.
                    var value = converter.ConvertFromInvariantString(arg.Value);
                    propertyInfo.SetValue(instance, value, null);
                }
            }

            Action performAction = instance.Perform;
            InvokeFilters(instance, performAction);
        }

        private void InvokeFilters(
            HangFireJob jobInstance,
            Action performAction)
        {
            var commandAction = performAction;

            var entries = _filters.ToList();
            entries.Reverse();

            foreach (var entry in entries)
            {
                var currentEntry = entry;

                var filterContext = new ServerFilterContext(jobInstance, performAction);
                commandAction = () => currentEntry.ServerFilter(filterContext);
            }

            commandAction();
        }
    }
}
