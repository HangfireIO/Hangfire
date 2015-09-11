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
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Hangfire.Annotations;

namespace Hangfire.Common
{
    /// <summary>
    /// Represents an action that can be marshalled to another process to
    /// be performed.
    /// </summary>
    /// 
    /// <remarks>
    /// <para>The ability to serialize an action is the cornerstone of 
    /// marshalling it outside of a current process boundaries. We are leaving 
    /// behind all the tricky features, e.g. serializing lambdas or so, and 
    /// considering a simple method call information as a such an action,
    /// and using reflection to perform it.</para>
    /// 
    /// <para>Reflection-based method invocation requires an instance of
    /// the <see cref="MethodInfo"/> class, the arguments and an instance of 
    /// the type on which to invoke the method (unless it is static). Since
    /// the same <see cref="MethodInfo"/> instance can be shared across
    /// multiple types (especially when they are defined in interfaces),
    /// we require to explicitly specify a corresponding <see cref="Type"/>
    /// instance to avoid any ambiguities to uniquely determine which type
    /// contains the method to be called.</para>
    /// 
    /// <para>The tuple Type/MethodInfo/Arguments can be easily serialized 
    /// and deserialized back.</para>
    /// </remarks>
    /// 
    /// <seealso cref="Server.IJobPerformanceProcess"/>
    /// 
    /// <threadsafety static="true" instance="false" />
    public partial class Job
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Job"/> class with the 
        /// given method metadata and no arguments. Its declaring type will be
        /// used to describe a type that contains the method.
        /// </summary>
        /// <param name="method">Method that supposed to be invoked.</param>
        public Job([NotNull] MethodInfo method)
            : this(method, new object[0])
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Job"/> class with the
        /// given method metadata and specified list of arguments. The type 
        /// that declares the method will be used to describe a type that 
        /// contains the method.
        /// </summary>
        /// <param name="method">Method that supposed to be invoked.</param>
        /// <param name="arguments">Arguments that will be passed to a method invocation.</param>
        public Job([NotNull] MethodInfo method, [NotNull] params object[] arguments)
            // ReSharper disable once AssignNullToNotNullAttribute
            : this(method.DeclaringType, method, arguments)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Job"/> class with the
        /// given method metadata, 
        /// a given method data and an empty arguments array.
        /// </summary>
        /// 
        /// <remarks>
        /// Each argument should be serialized into a string using the 
        /// <see cref="JobHelper.ToJson(object)"/> method of the <see cref="JobHelper"/> 
        /// class.
        /// </remarks>
        /// 
        /// <exception cref="ArgumentNullException"><paramref name="type"/> argument is null.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="method"/> argument is null.</exception>
        /// <exception cref="ArgumentException">Method contains unassigned generic type parameters.</exception>
        public Job([NotNull] Type type, [NotNull] MethodInfo method)
            : this(type, method, new object[0])
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Job"/> class with
        /// a given method data and arguments.
        /// </summary>
        /// 
        /// <remarks>
        /// Each argument should be serialized into a string using the 
        /// <see cref="JobHelper.ToJson(object)"/> method of the <see cref="JobHelper"/> 
        /// class.
        /// </remarks>
        /// 
        /// <exception cref="ArgumentNullException"><paramref name="type"/> argument is null.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="method"/> argument is null.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="args"/> argument is null.</exception>
        /// <exception cref="ArgumentException">Method contains unassigned generic type parameters.</exception>
        public Job([NotNull] Type type, [NotNull] MethodInfo method, [NotNull] params object[] args)
        {
            if (type == null) throw new ArgumentNullException("type");
            if (method == null) throw new ArgumentNullException("method");
            if (args == null) throw new ArgumentNullException("args");

            Validate(type, "type", method, "method", args.Length, "args");

            Type = type;
            Method = method;
            Args = args;
        }

        /// <summary>
        /// Gets the metadata of a type that contains a method that supposed 
        /// to be invoked during the performance.
        /// </summary>
        [NotNull]
        public Type Type { get; private set; }

        /// <summary>
        /// Gets the metadata of a method that supposed to be invoked during
        /// the performance.
        /// </summary>
        [NotNull]
        public MethodInfo Method { get; private set; }

        /// <summary>
        /// Gets a collection of arguments that will be passed to a method 
        /// invocation during the performance.
        /// </summary>
        [NotNull]
        public IReadOnlyList<object> Args { get; private set; }
            
        public override string ToString()
        {
            return String.Format("{0}.{1}", Type.ToGenericTypeString(), Method.Name);
        }

        internal IEnumerable<JobFilterAttribute> GetTypeFilterAttributes(bool useCache)
        {
            return useCache
                ? ReflectedAttributeCache.GetTypeFilterAttributes(Type)
                : GetFilterAttributes(Type);
        }

        internal IEnumerable<JobFilterAttribute> GetMethodFilterAttributes(bool useCache)
        {
            return useCache
                ? ReflectedAttributeCache.GetMethodFilterAttributes(Method)
                : GetFilterAttributes(Method);
        }

        private static IEnumerable<JobFilterAttribute> GetFilterAttributes(MemberInfo memberInfo)
        {
            return memberInfo
                .GetCustomAttributes(typeof(JobFilterAttribute), inherit: true)
                .Cast<JobFilterAttribute>();
        }

        /// <summary>
        /// Creates a new instance of the <see cref="Job"/> class on a 
        /// basis of the given static method call expression.
        /// </summary>
        /// 
        /// <exception cref="ArgumentNullException"><paramref name="methodCall"/> argument is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="methodCall"/> expression body does not contain <see cref="MethodCallExpression"/>.</exception>
        public static Job FromExpression([InstantHandle] Expression<Action> methodCall)
        {
            if (methodCall == null) throw new ArgumentNullException("methodCall");

            var callExpression = methodCall.Body as MethodCallExpression;
            if (callExpression == null)
            {
                throw new NotSupportedException("Expression body should be of type `MethodCallExpression`");
            }

            Type type;

            if (callExpression.Object != null)
            {
                var objectValue = GetExpressionValue(callExpression.Object);
                if (objectValue == null)
                {
                    throw new InvalidOperationException("Expression object should not be null.");
                }

                type = objectValue.GetType();
            }
            else
            {
                type = callExpression.Method.DeclaringType;
            }

            // Static methods can not be overridden in the derived classes, 
            // so we can take the method's declaring type.
            return new Job(
                // ReSharper disable once AssignNullToNotNullAttribute
                type,
                callExpression.Method,
                GetExpressionValues(callExpression.Arguments));
        }

        /// <summary>
        /// Creates a new instance of the <see cref="Job"/> class on a 
        /// basis of the given instance method call expression.
        /// </summary>
        /// 
        /// <exception cref="ArgumentNullException"><paramref name="methodCall"/> argument is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="methodCall"/> expression body does not contain <see cref="MethodCallExpression"/>.</exception>
        public static Job FromExpression<T>([InstantHandle] Expression<Action<T>> methodCall)
        {
            if (methodCall == null) throw new ArgumentNullException("methodCall");

            var callExpression = methodCall.Body as MethodCallExpression;
            if (callExpression == null)
            {
                throw new NotSupportedException("Expression body should be of type `MethodCallExpression`");
            }

            return new Job(
                typeof(T),
                callExpression.Method,
                GetExpressionValues(callExpression.Arguments));
        }

        private static void Validate(
            Type type, 
            [InvokerParameterName] string typeParameterName,
            MethodInfo method, 
            [InvokerParameterName] string methodParameterName,
            // ReSharper disable once UnusedParameter.Local
            int argumentCount,
            [InvokerParameterName] string argumentParameterName)
        {
            if (method.ContainsGenericParameters)
            {
                throw new ArgumentException("Job method can not contain unassigned generic type parameters.", methodParameterName);
            }

            if (method.DeclaringType == null)
            {
                throw new NotSupportedException("Global methods are not supported. Use class methods instead.");
            }

            if (!method.DeclaringType.IsAssignableFrom(type))
            {
                throw new ArgumentException(
                    String.Format("The type `{0}` must be derived from the `{1}` type.", method.DeclaringType, type),
                    typeParameterName);
            }

            if (!method.IsPublic)
            {
                throw new NotSupportedException("Only public methods can be invoked in the background.");
            }

            if (typeof(Task).IsAssignableFrom(method.ReturnType))
            {
                throw new NotSupportedException("Async methods are not supported. Please make them synchronous before using them in background.");
            }

            var parameters = method.GetParameters();

            if (parameters.Length != argumentCount)
            {
                throw new ArgumentException(
                    "Argument count must be equal to method parameter count.",
                    argumentParameterName);
            }

            foreach (var parameter in parameters)
            {
                // There is no guarantee that specified method will be invoked
                // in the same process. Therefore, output parameters and parameters
                // passed by reference are not supported.

                if (parameter.IsOut)
                {
                    throw new NotSupportedException(
                        "Output parameters are not supported: there is no guarantee that specified method will be invoked inside the same process.");
                }

                if (parameter.ParameterType.IsByRef)
                {
                    throw new NotSupportedException(
                        "Parameters, passed by reference, are not supported: there is no guarantee that specified method will be invoked inside the same process.");
                }
            }
        }

        private static object[] GetExpressionValues(IEnumerable<Expression> expressions)
        {
            return expressions.Select(GetExpressionValue).ToArray();
        }

        private static object GetExpressionValue(Expression expression)
        {
            var constantExpression = expression as ConstantExpression;

            return constantExpression != null
                ? constantExpression.Value
                : CachedExpressionCompiler.Evaluate(expression);
        }
    }
}
