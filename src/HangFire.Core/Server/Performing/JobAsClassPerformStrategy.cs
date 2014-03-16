// This file is part of HangFire.
// Copyright © 2013-2014 Sergey Odinokov.
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
using HangFire.Common;

namespace HangFire.Server.Performing
{
    internal class JobAsClassPerformStrategy : IJobPerformStrategy
    {
        private readonly JobActivator _activator = JobActivator.Current;

        private readonly MethodData _methodData;
        private readonly Dictionary<string, string> _arguments;

        public JobAsClassPerformStrategy(
            MethodData methodData,
            Dictionary<string, string> arguments)
        {
            if (methodData == null) throw new ArgumentNullException("methodData");
            if (arguments == null) throw new ArgumentNullException("arguments");

            _methodData = methodData;
            _arguments = arguments;
        }

        public void Perform()
        {
            BackgroundJob instance = null;

            try
            {
                instance = (BackgroundJob)_activator.ActivateJob(_methodData.Type);

                if (instance == null)
                {
                    throw new InvalidOperationException(
                        String.Format("JobActivator returned NULL instance of the '{0}' type.", _methodData.Type));
                }

                InitializeProperties(instance, _arguments);

                try
                {
                    instance.Perform();
                }
                catch (Exception ex)
                {
                    throw new JobPerformanceException(
                        "An exception occured during performance of the job",
                        ex);
                }
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
                var propertyInfo = _methodData.Type.GetProperty(arg.Key);
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
                                _methodData.Type),
                            ex);
                    }
                }
            }
        }
    }
}