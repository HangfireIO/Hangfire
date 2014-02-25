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
using System.Linq;
using System.Reflection;
using HangFire.Common.Filters;

namespace HangFire.Common
{
    /// <summary>
    /// Represents information about type and method that will be called during
    /// the performance of a job. Provides internal methods for serialization 
    /// and deserialization of this information.
    /// </summary>
    /// 
    /// <remarks>
    /// Information about method that will be called consist of a 
    /// <see cref="JobMethod.Type"/> and a <see cref="JobMethod.Method"/>.
    /// Although there is the <see cref="MethodInfo.DeclaringType"/> property,
    /// this class allows you to set a class that contains the given method
    /// explicitly, enabling you to choose not only the base class, but one
    /// of its successors.
    /// </remarks>
    public class JobMethod
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JobMethod"/> class,
        /// using a given <paramref name="type"/>. Resulting instance would
        /// correspond to the Old Job Format. This constructor is used only
        /// for compatibility with the Old Client API.
        /// TODO: remove this constructor before 1.0
        /// </summary>
        /// <param name="type">Successor of the <see cref="BackgroundJob"/> class.</param>
        [Obsolete("This constructor will be removed before 1.0. Use the new version of the Client API.")]
        public JobMethod(Type type)
        {
            if (type == null) throw new ArgumentNullException("type");

            if (!typeof (BackgroundJob).IsAssignableFrom(type))
            {
                throw new ArgumentException("Given type must be a successor of the `HangFire.BackgroundJob` class.", "type");
            }

            Type = type;
            OldFormat = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JobMethod"/> class,
        /// using a given <paramref name="method"/> and specified 
        /// <paramref name="type"/> that contains the method.
        /// </summary>
        /// <param name="type">The type that contains the <paramref name="method"/>.
        /// </param> <param name="method"> Method to be invoked during the job performance. </param>
        public JobMethod(Type type, MethodInfo method)
        {
            if (type == null) throw new ArgumentNullException("type");
            if (method == null) throw new ArgumentNullException("method");

            if (method.DeclaringType == null)
            {
                throw new NotSupportedException("Global methods are not supported. Use class methods instead.");
            }

            if (!method.DeclaringType.IsAssignableFrom(type))
            {
                throw new ArgumentException(
                    "The type `{0}` must be derived from the `{1}` type.", "type");
            }

            Type = type;
            Method = method;
        }

        /// <summary>
        /// Gets an instance of <see cref="System.Type"/> class that contains 
        /// the given <see cref="Method"/>. It can be both the type that declares the
        /// method as well as its successor.
        /// </summary>
        public Type Type { get; private set; }

        /// <summary>
        /// Gets an instance of the <see cref="MethodInfo"/> class that points
        /// to the method that will be called during the performance of a job.
        /// </summary>
        public MethodInfo Method { get; private set; }
        
        /// <summary>
        /// Gets wheither this instance contains the information in the
        /// Old Job Format.
        /// TODO: remove it before 1.0
        /// </summary>
        [Obsolete("This property will be removed before 1.0. Use the new version of the Client API.")]
        public bool OldFormat { get; private set; }

        public Dictionary<string, string> Serialize()
        {
            if (OldFormat)
            {
                return new Dictionary<string, string>
                {
                    { "Type", Type.AssemblyQualifiedName },
                };
            }

            return new Dictionary<string, string>
            {
                { "Type", Type.AssemblyQualifiedName },
                { "Method", Method.Name },
                { "ParameterTypes", JobHelper.ToJson(Method.GetParameters().Select(x => x.ParameterType).ToArray()) },
            };
        }

        public static JobMethod Deserialize(Dictionary<string, string> job)
        {
            if (job == null) throw new ArgumentNullException("job");

            var oldFormat = false;

            // Normalize a job in the old format.
            if (!job.ContainsKey("Method") || job["Method"] == null)
            {
                // Avoid to modify the original dictionary.
                job = new Dictionary<string, string>(job);
                job["Method"] = "Perform";
                job["ParameterTypes"] = JobHelper.ToJson(new Type[0]);

                oldFormat = true;
            }

            try
            {
                var type = Type.GetType(job["Type"], throwOnError: true, ignoreCase: true);
                var parameterTypes = JobHelper.FromJson<Type[]>(job["ParameterTypes"]);
                var method = type.GetMethod(job["Method"], parameterTypes);

                if (method == null)
                {
                    throw new InvalidOperationException(String.Format(
                        "The type `{0}` does not contain a method with signature `{1}({2})`",
                        type.FullName,
                        job["Method"],
                        String.Join(", ", parameterTypes.Select(x => x.Name))));
                }

                return new JobMethod(type, method) { OldFormat = oldFormat };
            }
            catch (Exception ex)
            {
                throw new JobLoadException("Could not load the job. See inner exception for the details.", ex);
            }
        }

        internal IEnumerable<JobFilterAttribute> GetTypeFilterAttributes(bool useCache)
        {
            return useCache
                ? ReflectedAttributeCache.GetTypeFilterAttributes(Type)
                : GetFilterAttributes(Type);
        }

        internal IEnumerable<JobFilterAttribute> GetMethodFilterAttributes(bool useCache)
        {
            if (Method == null)
            {
                return Enumerable.Empty<JobFilterAttribute>();
            }

            return useCache
                ? ReflectedAttributeCache.GetMethodFilterAttributes(Method)
                : GetFilterAttributes(Method);
        }

        private IEnumerable<JobFilterAttribute> GetFilterAttributes(MemberInfo memberInfo)
        {
            return memberInfo
                .GetCustomAttributes(typeof(JobFilterAttribute), inherit: true)
                .Cast<JobFilterAttribute>();
        }
    }
}