// This file is part of Hangfire. Copyright © 2019 Hangfire OÜ.
// 
// Hangfire is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as 
// published by the Free Software Foundation, either version 3 
// of the License, or any later version.
// 
// Hangfire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public 
// License along with Hangfire. If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Reflection;
using Hangfire.Common;
using Hangfire.Server;

namespace Hangfire
{
    public class JobParameterInjectionFilter : IServerFilter
    {
        public void OnPerforming(PerformingContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            var argumentsArray = context.BackgroundJob.Job?.Args as object[];
            if (argumentsArray == null) return;

            var parameters = context.BackgroundJob.Job.Method.GetParameters();

            for (var index = 0; index < parameters.Length; index++)
            {
                var attribute = parameters[index].GetCustomAttribute<FromParameterAttribute>();
                if (attribute == null) continue;

                var parameterType = parameters[index].ParameterType;
                var parameterName = attribute.ParameterName;

                if (String.IsNullOrEmpty(parameterName) || argumentsArray[index] != null) continue;

                var serialized = context.Connection.GetJobParameter(context.BackgroundJob.Id, parameterName);
                if (serialized == null) continue;

                argumentsArray[index] = SerializationHelper.Deserialize(serialized, parameterType, SerializationOption.User);
            }
        }

        public void OnPerformed(PerformedContext context)
        {
        }
    }
}