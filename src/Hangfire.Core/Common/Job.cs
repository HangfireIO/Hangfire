﻿// This file is part of Hangfire.
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
using Hangfire.Annotations;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

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
    /// behind all the tricky features, e.g. serializing lambdas with their
    /// closures or so, and considering a simple method call information as 
    /// a such an action, and using reflection to perform it.</para>
    /// 
    /// <para>Reflection-based method invocation requires an instance of
    /// the <see cref="MethodInfo"/> class, the arguments and an instance of 
    /// the type on which to invoke the method (unless it is static). Since the
    /// same <see cref="MethodInfo"/> instance can be shared across multiple 
    /// types (especially when they are defined in interfaces), we also allow 
    /// to specify a <see cref="Type"/> that contains the defined method 
    /// explicitly for better flexibility.</para>
    /// 
    /// <para>Marshalling imposes restrictions on a method that should be 
    /// performed:</para>
    /// 
    /// <list type="bullet">
    ///     <item>Method should be public.</item>
    ///     <item>Method should not contain <see langword="out"/> and <see langword="ref"/> parameters.</item>
    ///     <item>Method should not contain open generic parameters.</item>
    /// </list>
    /// </remarks>
    /// 
    /// <example>
    /// <para>The following example demonstrates the creation of a <see cref="Job"/>
    /// type instances using expression trees. This is the recommended way of
    /// creating jobs.</para>
    /// 
    /// <code lang="cs" source="..\Samples\Job.cs" region="Supported Methods" />
    /// 
    /// <para>The next example demonstrates unsupported methods. Any attempt
    /// to create a job based on these methods fails with 
    /// <see cref="NotSupportedException"/>.</para>
    /// 
    /// <code lang="cs" source="..\Samples\Job.cs" region="Unsupported Methods" />
    /// </example>
    /// 
    /// <seealso cref="IBackgroundJobClient"/>
    /// <seealso cref="Server.IBackgroundJobPerformer"/>
    /// 
    /// <threadsafety static="true" instance="false" />
    public partial class Job
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Job"/> class with the
        /// metadata of a method with no arguments.
        /// </summary>
        /// 
        /// <param name="method">Method that should be invoked.</param>
        /// 
        /// <exception cref="ArgumentNullException"><paramref name="method"/> argument is null.</exception>
        /// <exception cref="NotSupportedException"><paramref name="method"/> is not supported.</exception>
        public Job([NotNull] MethodInfo method)
            : this(method, new object[0])
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Job"/> class with the
        /// metadata of a method and the given list of arguments.
        /// </summary>
        /// 
        /// <param name="method">Method that should be invoked.</param>
        /// <param name="args">Arguments that will be passed to a method invocation.</param>
        /// 
        /// <exception cref="ArgumentNullException"><paramref name="method"/> argument is null.</exception>
        /// <exception cref="ArgumentException">Parameter/argument count mismatch.</exception>
        /// <exception cref="NotSupportedException"><paramref name="method"/> is not supported.</exception>
        public Job([NotNull] MethodInfo method, [NotNull] params object[] args)
            // ReSharper disable once AssignNullToNotNullAttribute
            : this(method.DeclaringType, method, args)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Job"/> class with the
        /// type, metadata of a method with no arguments.
        /// </summary>
        /// 
        /// <param name="type">Type that contains the given method.</param>
        /// <param name="method">Method that should be invoked.</param>
        /// 
        /// <exception cref="ArgumentNullException"><paramref name="type"/> argument is null.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="method"/> argument is null.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="type"/> does not contain the given <paramref name="method"/>.
        /// </exception>
        /// <exception cref="ArgumentException">Parameter/argument count mismatch.</exception>
        /// <exception cref="NotSupportedException"><paramref name="method"/> is not supported.</exception>
        public Job([NotNull] Type type, [NotNull] MethodInfo method)
            : this(type, method, new object[0])
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Job"/> class with the 
        /// type, metadata of a method and the given list of arguments.
        /// </summary>
        /// 
        /// <param name="type">Type that contains the given method.</param>
        /// <param name="method">Method that should be invoked.</param>
        /// <param name="args">Arguments that should be passed during the method call.</param>
        /// 
        /// <exception cref="ArgumentNullException"><paramref name="type"/> argument is null.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="method"/> argument is null.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="args"/> argument is null.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="type"/> does not contain the given <paramref name="method"/>.
        /// </exception>
        /// <exception cref="ArgumentException">Parameter/argument count mismatch.</exception>
        /// <exception cref="NotSupportedException"><paramref name="method"/> is not supported.</exception>
        public Job([NotNull] Type type, [NotNull] MethodInfo method, [NotNull] params object[] args)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            if (method == null) throw new ArgumentNullException(nameof(method));
            if (args == null) throw new ArgumentNullException(nameof(args));
            
            Validate(type, nameof(type), method, nameof(method), args.Length, nameof(args));

            Type = type;
            Method = method;
            Args = args;
        }

        /// <summary>
        /// Gets the metadata of a type that contains a method that should be 
        /// invoked during the performance.
        /// </summary>
        [NotNull]
        public Type Type { get; }

        /// <summary>
        /// Gets the metadata of a method that should be invoked during the 
        /// performance.
        /// </summary>
        [NotNull]
        public MethodInfo Method { get; }

        /// <summary>
        /// Gets a read-only collection of arguments that Should be passed to a 
        /// method invocation during the performance.
        /// </summary>
        [NotNull]
        public IReadOnlyList<object> Args { get; }
        
        public override string ToString()
        {
            return $"{Type.ToGenericTypeString()}.{Method.Name}";
        }

        internal IEnumerable<JobFilterAttribute> GetTypeFilterAttributes(bool useCache)
        {
            return useCache
                ? ReflectedAttributeCache.GetTypeFilterAttributes(Type)
                : GetFilterAttributes(Type.GetTypeInfo());
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
        /// Gets a new instance of the <see cref="Job"/> class based on the
        /// given expression tree of a method call.
        /// </summary>
        /// 
        /// <param name="methodCall">Expression tree of a method call.</param>
        /// 
        /// <exception cref="ArgumentNullException"><paramref name="methodCall"/> is null.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="methodCall"/> expression body is not of type 
        /// <see cref="MethodCallExpression"/>.</exception>
        /// <exception cref="NotSupportedException"><paramref name="methodCall"/> 
        /// expression contains a method that is not supported.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="methodCall"/>
        /// instance object of a given expression points to <see langword="null"/>.
        /// </exception>
        /// 
        /// <remarks>
        /// <para>The <see cref="Job.Type"/> property of a returning job will 
        /// point to the type of a given instance object when it is specified, 
        /// or to the declaring type otherwise. All the arguments are evaluated 
        /// using the expression compiler that uses caching where possible to 
        /// decrease the performance penalty.</para>
        /// 
        /// <note>Instance object (e.g. <c>() => instance.Method()</c>) is 
        /// <b>only used to obtain the type</b> for a job. It is not
        /// serialized and not passed across the process boundaries.</note>
        /// </remarks>
        public static Job FromExpression([NotNull, InstantHandle] Expression<Action> methodCall)
        {
            return FromExpression(methodCall, null);
        }

        /// <summary>
        /// Gets a new instance of the <see cref="Job"/> class based on the
        /// given expression tree of a method call.
        /// </summary>
        /// 
        /// <param name="methodCall">Expression tree of a method call.</param>
        /// 
        /// <exception cref="ArgumentNullException"><paramref name="methodCall"/> is null.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="methodCall"/> expression body is not of type 
        /// <see cref="MethodCallExpression"/>.</exception>
        /// <exception cref="NotSupportedException"><paramref name="methodCall"/> 
        /// expression contains a method that is not supported.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="methodCall"/>
        /// instance object of a given expression points to <see langword="null"/>.
        /// </exception>
        /// 
        /// <remarks>
        /// <para>The <see cref="Job.Type"/> property of a returning job will 
        /// point to the type of a given instance object when it is specified, 
        /// or to the declaring type otherwise. All the arguments are evaluated 
        /// using the expression compiler that uses caching where possible to 
        /// decrease the performance penalty.</para>
        /// 
        /// <note>Instance object (e.g. <c>() => instance.Method()</c>) is 
        /// <b>only used to obtain the type</b> for a job. It is not
        /// serialized and not passed across the process boundaries.</note>
        /// </remarks>
        public static Job FromExpression([NotNull, InstantHandle] Expression<Func<Task>> methodCall)
        {
            return FromExpression(methodCall, null);
        }

        /// <summary>
        /// Gets a new instance of the <see cref="Job"/> class based on the
        /// given expression tree of an instance method call with explicit
        /// type specification.
        /// </summary>
        /// <typeparam name="TType">Explicit type that should be used on method call.</typeparam>
        /// <param name="methodCall">Expression tree of a method call on <typeparamref name="TType"/>.</param>
        /// 
        /// <exception cref="ArgumentNullException"><paramref name="methodCall"/> is null.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="methodCall"/> expression body is not of type 
        /// <see cref="MethodCallExpression"/>.</exception>
        /// <exception cref="NotSupportedException"><paramref name="methodCall"/> 
        /// expression contains a method that is not supported.</exception>
        /// 
        /// <remarks>
        /// <para>All the arguments are evaluated using the expression compiler
        /// that uses caching where possible to decrease the performance 
        /// penalty.</para>
        /// </remarks>
        public static Job FromExpression<TType>([NotNull, InstantHandle] Expression<Action<TType>> methodCall)
        {
            return FromExpression(methodCall, typeof(TType));
        }

        /// <summary>
        /// Gets a new instance of the <see cref="Job"/> class based on the
        /// given expression tree of an instance method call with explicit
        /// type specification.
        /// </summary>
        /// <typeparam name="TType">Explicit type that should be used on method call.</typeparam>
        /// <param name="methodCall">Expression tree of a method call on <typeparamref name="TType"/>.</param>
        /// 
        /// <exception cref="ArgumentNullException"><paramref name="methodCall"/> is null.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="methodCall"/> expression body is not of type 
        /// <see cref="MethodCallExpression"/>.</exception>
        /// <exception cref="NotSupportedException"><paramref name="methodCall"/> 
        /// expression contains a method that is not supported.</exception>
        /// 
        /// <remarks>
        /// <para>All the arguments are evaluated using the expression compiler
        /// that uses caching where possible to decrease the performance 
        /// penalty.</para>
        /// </remarks>
        public static Job FromExpression<TType>([NotNull, InstantHandle] Expression<Func<TType, Task>> methodCall)
        {
            return FromExpression(methodCall, typeof(TType));
        }

        private static Job FromExpression([NotNull] LambdaExpression methodCall, [CanBeNull] Type explicitType)
        {
            if (methodCall == null) throw new ArgumentNullException(nameof(methodCall));

            var callExpression = methodCall.Body as MethodCallExpression;
            if (callExpression == null)
            {
                throw new ArgumentException("Expression body should be of type `MethodCallExpression`", nameof(methodCall));
            }

            var type = explicitType ?? callExpression.Method.DeclaringType;
            var method = callExpression.Method;

            if (explicitType == null && callExpression.Object != null)
            {
                // Creating a job that is based on a scope variable. We should infer its
                // type and method based on its value, and not from the expression tree.

                // TODO: BREAKING: Consider removing this special case entirely.
                // People consider that the whole object is serialized, this is not true.

                var objectValue = GetExpressionValue(callExpression.Object);
                if (objectValue == null)
                {
                    throw new InvalidOperationException("Expression object should be not null.");
                }

                // TODO: BREAKING: Consider using `callExpression.Object.Type` expression instead.
                type = objectValue.GetType();

                // If an expression tree is based on interface, we should use its own
                // MethodInfo instance, based on the same method name and parameter types.
                method = type.GetNonOpenMatchingMethod(
                    callExpression.Method.Name,
                    callExpression.Method.GetParameters().Select(x => x.ParameterType).ToArray());
            }

            return new Job(
                // ReSharper disable once AssignNullToNotNullAttribute
                type,
                method,
                GetExpressionValues(callExpression.Arguments));
        }

        private static void Validate(
            Type type, 
            [InvokerParameterName] string typeParameterName,
            MethodInfo method, 
            // ReSharper disable once UnusedParameter.Local
            [InvokerParameterName] string methodParameterName,
            // ReSharper disable once UnusedParameter.Local
            int argumentCount,
            [InvokerParameterName] string argumentParameterName)
        {
            if (!method.IsPublic)
            {
                throw new NotSupportedException("Only public methods can be invoked in the background.");
            }

            if (method.ContainsGenericParameters)
            {
                throw new NotSupportedException("Job method can not contain unassigned generic type parameters.");
            }

            if (method.DeclaringType == null)
            {
                throw new NotSupportedException("Global methods are not supported. Use class methods instead.");
            }

            if (!method.DeclaringType.GetTypeInfo().IsAssignableFrom(type.GetTypeInfo()))
            {
                throw new ArgumentException(
                    $"The type `{method.DeclaringType}` must be derived from the `{type}` type.",
                    typeParameterName);
            }

            if (method.ReturnType == typeof(void) && method.GetCustomAttribute<AsyncStateMachineAttribute>() != null)
            {
                throw new NotSupportedException("Async void methods are not supported. Use async Task instead.");
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

                var parameterTypeInfo = parameter.ParameterType.GetTypeInfo();
                
                if (parameterTypeInfo.IsSubclassOf(typeof(Delegate)) || parameterTypeInfo.IsSubclassOf(typeof(Expression)))
                {
                    throw new NotSupportedException(
                        "Anonymous functions, delegates and lambda expressions aren't supported in job method parameters: it's very hard to serialize them and all their scope in general.");
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
