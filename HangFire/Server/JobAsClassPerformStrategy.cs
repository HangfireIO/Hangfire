// This file is part of HangFire.
// Copyright © 2013 Sergey Odinokov.
// 
// HangFire is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// HangFire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with HangFire.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using HangFire.Client;

namespace HangFire.Server
{
    internal class JobAsClassPerformStrategy : IJobPerformStrategy
    {
        private readonly JobActivator _activator;
        private readonly JobMethod _method;
        private readonly Dictionary<string, string> _arguments;

        public JobAsClassPerformStrategy(
            JobActivator activator,
            JobMethod method,
            Dictionary<string, string> arguments)
        {
            if (activator == null) throw new ArgumentNullException("activator");
            if (method == null) throw new ArgumentNullException("method");
            if (arguments == null) throw new ArgumentNullException("arguments");

            _activator = activator;
            _method = method;
            _arguments = arguments;
        }

        public void Perform()
        {
            BackgroundJob instance = null;

            try
            {
                instance = (BackgroundJob)_activator.ActivateJob(_method.Type);

                if (instance == null)
                {
                    throw new InvalidOperationException(
                        String.Format("JobActivator returned NULL instance of the '{0}' type.", _method.Type));
                }

                InitializeProperties(instance, _arguments);
                instance.Perform();
            }
            finally
            {
                var disposable = instance as IDisposable;
                if (disposable != null)
                {
                    disposable.Dispose();
                }
            }
        }

        private void InitializeProperties(BackgroundJob instance, Dictionary<string, string> arguments)
        {
            foreach (var arg in arguments)
            {
                var propertyInfo = _method.Type.GetProperty(arg.Key);
                if (propertyInfo != null)
                {
                    var converter = TypeDescriptor.GetConverter(propertyInfo.PropertyType);

                    try
                    {
                        var value = converter.ConvertFromInvariantString(arg.Value);
                        propertyInfo.SetValue(instance, value, null);
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException(
                            String.Format(
                                "Could not set the property '{0}' of the instance of class '{1}'. See the inner exception for details.",
                                propertyInfo.Name,
                                _method.Type),
                            ex);
                    }
                }
            }
        }
    }
}