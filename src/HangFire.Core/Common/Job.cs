using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;

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
        public Job(JobMethod methodData, IEnumerable<string> arguments)
        {
            if (methodData == null) throw new ArgumentNullException("methodData");

            MethodData = methodData;
            Arguments = arguments ?? Enumerable.Empty<string>();
        }

        /// <summary>
        /// Gets the information about a method that will be performed in background.
        /// </summary>
        public JobMethod MethodData { get; private set; }

        /// <summary>
        /// Gets arguments collection that will be passed to the method.
        /// </summary>
        public IEnumerable<string> Arguments { get; private set; }

        /// <summary>
        /// Creates a new instance of the <see cref="Job"/> class on a 
        /// basis of the given static method call expression.
        /// </summary>
        public static Job FromExpression(Expression<Action> methodCall)
        {
            if (methodCall == null) throw new ArgumentNullException("methodCall");

            var callExpression = methodCall.Body as MethodCallExpression;
            if (callExpression == null)
            {
                throw new ArgumentException("Expression body should be of type `MethodCallExpression`", "methodCall");
            }

            return new Job(JobMethod.FromExpression(methodCall), GetArguments(callExpression));
        }

        /// <summary>
        /// Creates a new instance of the <see cref="Job"/> class on a 
        /// basis of the given instance method call expression.
        /// </summary>
        public static Job FromExpression<T>(Expression<Action<T>> methodCall)
        {
            if (methodCall == null) throw new ArgumentNullException("methodCall");

            var callExpression = methodCall.Body as MethodCallExpression;
            if (callExpression == null)
            {
                throw new ArgumentException("Expression body should be of type `MethodCallExpression`", "methodCall");
            }

            return new Job(JobMethod.FromExpression(methodCall), GetArguments(callExpression));
        }

        private static IEnumerable<string> GetArguments(MethodCallExpression callExpression)
        {
            if (callExpression == null) throw new ArgumentNullException("callExpression");

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
            var constantExpression = expression as ConstantExpression;

            if (constantExpression != null)
            {
                return constantExpression.Value;
            }

            return CachedExpressionCompiler.Evaluate(expression);
        }
    }
}
