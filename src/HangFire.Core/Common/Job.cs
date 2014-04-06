// This file is part of HangFire.
// Copyright © 2013-2014 Sergey Odinokov.
// 
// HangFire is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as 
// published by the Free Software Foundation, either version 3 
// of the License, or any later version.
// 
// HangFire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public 
// License along with HangFire. If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace HangFire.Common
{
    /// <summary>
    /// Represents the information about background invocation of a method.
    /// </summary>
    public class Job
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Job"/> class with
        /// a given method data and arguments.
        /// </summary>
        /// 
        /// <remarks>
        /// Each argument should be serialized into a string using the 
        /// <see cref="TypeConverter.ConvertToInvariantString(object)"/> method of
        /// a corresponding <see cref="TypeConverter"/> instance.
        /// </remarks>
        /// 
        /// <param name="methodData">Method that will be called during the performance of the job.</param>
        /// <param name="arguments">Serialized arguments that will be passed to a <paramref name="methodData"/>.</param>
        /// 
        /// <exception cref="ArgumentNullException"><paramref name="methodData"/> argument is null.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="arguments"/> argument is null.</exception>
        public Job(MethodData methodData, string[] arguments)
        {
            if (methodData == null) throw new ArgumentNullException("methodData");
            if (arguments == null) throw new ArgumentNullException("arguments");

            ValidateMethod(methodData.MethodInfo);

            MethodData = methodData;
            Arguments = arguments;
        }

        /// <summary>
        /// Gets the information about a method that will be performed in background.
        /// </summary>
        public MethodData MethodData { get; private set; }

        /// <summary>
        /// Gets arguments array that will be passed to the method during its invocation.
        /// </summary>
        public string[] Arguments { get; private set; }

        /// <summary>
        /// Creates a new instance of the <see cref="Job"/> class on a 
        /// basis of the given static method call expression.
        /// </summary>
        /// 
        /// <exception cref="ArgumentNullException"><paramref name="methodCall"/> argument is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="methodCall"/> expression body does not contain <see cref="MethodCallExpression"/>.</exception>
        public static Job FromExpression(Expression<Action> methodCall)
        {
            if (methodCall == null) throw new ArgumentNullException("methodCall");

            var callExpression = methodCall.Body as MethodCallExpression;
            if (callExpression == null)
            {
                throw new NotSupportedException("Expression body should be of type `MethodCallExpression`");
            }

            return new Job(MethodData.FromExpression(methodCall), GetArguments(callExpression));
        }

        /// <summary>
        /// Creates a new instance of the <see cref="Job"/> class on a 
        /// basis of the given instance method call expression.
        /// </summary>
        /// 
        /// <exception cref="ArgumentNullException"><paramref name="methodCall"/> argument is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="methodCall"/> expression body does not contain <see cref="MethodCallExpression"/>.</exception>
        public static Job FromExpression<T>(Expression<Action<T>> methodCall)
        {
            if (methodCall == null) throw new ArgumentNullException("methodCall");

            var callExpression = methodCall.Body as MethodCallExpression;
            if (callExpression == null)
            {
                throw new NotSupportedException("Expression body should be of type `MethodCallExpression`");
            }

            return new Job(MethodData.FromExpression(methodCall), GetArguments(callExpression));
        }

        private static void ValidateMethod(MethodBase methodInfo)
        {
            if (!methodInfo.IsPublic)
            {
                throw new NotSupportedException("Only public methods can be invoked in the background.");
            }

            var parameters = methodInfo.GetParameters();

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

        private static string[] GetArguments(MethodCallExpression callExpression)
        {
            Debug.Assert(callExpression != null);

            var arguments = callExpression.Arguments.Select(GetArgumentValue).ToArray();

            var serializedArguments = new List<string>(arguments.Length);
            foreach (var argument in arguments)
            {
                string value = null;

                if (argument != null)
                {
                    var converter = TypeDescriptor.GetConverter(argument.GetType());
                    value = converter.ConvertToInvariantString(argument);
                }

                // Logic, related to optional parameters and their default values, 
                // can be skipped, because it is impossible to omit them in 
                // lambda-expressions (leads to a compile-time error).

                serializedArguments.Add(value);
            }

            return serializedArguments.ToArray();
        }

        private static object GetArgumentValue(Expression expression)
        {
            Debug.Assert(expression != null);

            var constantExpression = expression as ConstantExpression;

            if (constantExpression != null)
            {
                return constantExpression.Value;
            }

            return CachedExpressionCompiler.Evaluate(expression);
        }
    }
}
