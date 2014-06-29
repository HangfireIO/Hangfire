// This file is part of Hangfire.
// Copyright © 2013-2014 Sergey Odinokov.
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
using System.Linq;
using Hangfire.Common;

namespace Hangfire.Storage
{
    public class InvocationData
    {
        public InvocationData(
            string type, string method, string parameterTypes, string arguments)
        {
            Type = type;
            Method = method;
            ParameterTypes = parameterTypes;
            Arguments = arguments;
        }

        public string Type { get; private set; }
        public string Method { get; private set; }
        public string ParameterTypes { get; private set; }
        public string Arguments { get; set; }

        public Job Deserialize()
        {
            try
            {
                var type = System.Type.GetType(Type, throwOnError: true, ignoreCase: true);
                var parameterTypes = JobHelper.FromJson<Type[]>(ParameterTypes);
                var method = type.GetMethod(Method, parameterTypes);
                
                if (method == null)
                {
                    throw new InvalidOperationException(String.Format(
                        "The type `{0}` does not contain a method with signature `{1}({2})`",
                        type.FullName,
                        Method,
                        String.Join(", ", parameterTypes.Select(x => x.Name))));
                }

                var arguments = JobHelper.FromJson<string[]>(Arguments);

                return new Job(type, method, arguments);
            }
            catch (Exception ex)
            {
                throw new JobLoadException("Could not load the job. See inner exception for the details.", ex);
            }
        }

        public static InvocationData Serialize(Job job)
        {
            return new InvocationData(
                job.Type.AssemblyQualifiedName,
                job.Method.Name,
                JobHelper.ToJson(job.Method.GetParameters().Select(x => x.ParameterType)),
                JobHelper.ToJson(job.Arguments));
        }
    }
}
