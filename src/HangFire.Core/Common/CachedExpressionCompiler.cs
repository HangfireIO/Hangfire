// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace Hangfire.Common
{
    /// <summary>
    /// The caching expression tree compiler was copied from MVC core to MVC Futures so that Futures code could benefit
    /// from it and so that it could be exposed as a public API. This is the only public entry point into the system.
    /// See the comments in the ExpressionUtil namespace for more information.
    ///
    /// The unit tests for the ExpressionUtil.* types are in the System.Web.Mvc.Test project.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class CachedExpressionCompiler
    {
        private static readonly ParameterExpression UnusedParameterExpr = Expression.Parameter(typeof(object), "_unused");

        /// <summary>
        /// Evaluates an expression (not a LambdaExpression), e.g. 2 + 2.
        /// </summary>
        /// <param name="arg"></param>
        /// <returns>Expression result.</returns>
        public static object Evaluate(Expression arg)
        {
            if (arg == null)
            {
                throw new ArgumentNullException("arg");
            }

            Func<object, object> func = Wrap(arg);
            return func(null);
        }

        private static Func<object, object> Wrap(Expression arg)
        {
            var lambdaExpr = Expression.Lambda<Func<object, object>>(Expression.Convert(arg, typeof(object)), UnusedParameterExpr);
            return ExpressionUtil.CachedExpressionCompiler.Process(lambdaExpr);
        }
    }
}
