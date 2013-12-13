using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HangFire.Filters;

namespace HangFire.Client
{
    public class JobMethod
    {
        public JobMethod(Type type, MethodInfo method)
        {
            if (type == null) throw new ArgumentNullException("type");
            if (method == null) throw new ArgumentNullException("method");

            Type = type;
            Method = method;
        }

        public Type Type { get; private set; }
        public MethodInfo Method { get; private set; }

        // For compatibility with the Old Client API.
        // TODO: remove it in version 1.0
        public bool OldFormat { get; private set; }

        internal Dictionary<string, string> Serialize()
        {
            return new Dictionary<string, string>
            {
                { "Type", Type.AssemblyQualifiedName },
                { "Method", Method.Name },
                { "ParameterTypes", JobHelper.ToJson(Method.GetParameters().Select(x => x.ParameterType).ToArray()) },
            };
        }

        internal static JobMethod Deserialize(Dictionary<string, string> job)
        {
            if (job == null) throw new ArgumentNullException("job");

            var oldFormat = false;

            // For compatibility with the Old Client API
            // TODO: remove this in version 1.0
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

                return new JobMethod(type, method) { OldFormat = oldFormat };
            }
            catch (Exception ex)
            {
                throw new JobLoadException("Could not load the job. See inner exception for the details.", ex);
            }
        }

        internal IEnumerable<JobFilterAttribute> GetTypeFilterAttributes(bool useCache)
        {
            if (Type == null)
            {
                return Enumerable.Empty<JobFilterAttribute>();
            }

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