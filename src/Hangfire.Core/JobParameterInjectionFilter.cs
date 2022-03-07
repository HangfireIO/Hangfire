// This file is part of Hangfire. Copyright © 2019 Sergey Odinokov.
// 
// Permission to use, copy, modify, and/or distribute this software for any
// purpose with or without fee is hereby granted.
// 
// THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES WITH
// REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY
// AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY SPECIAL, DIRECT,
// INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM
// LOSS OF USE, DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR
// OTHER TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR
// PERFORMANCE OF THIS SOFTWARE.

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