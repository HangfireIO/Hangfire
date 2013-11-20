// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace HangFire
{
    // The caching expression tree compiler was copied from MVC core to MVC Futures so that Futures code could benefit
    // from it and so that it could be exposed as a public API. This is the only public entry point into the system.
    // See the comments in the ExpressionUtil namespace for more information.
    //
    // The unit tests for the ExpressionUtil.* types are in the System.Web.Mvc.Test project.
    public static class CachedExpressionCompiler
    {
        private static readonly ParameterExpression _unusedParameterExpr = Expression.Parameter(typeof(object), "_unused");

        // Implements caching around LambdaExpression.Compile() so that equivalent expression trees only have to be
        // compiled once.
        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "This is an appropriate nesting of generic types")]
        public static Func<TModel, TValue> Compile<TModel, TValue>(this Expression<Func<TModel, TValue>> lambdaExpression)
        {
            if (lambdaExpression == null)
            {
                throw new ArgumentNullException("lambdaExpression");
            }

            return HangFire.ExpressionUtil.CachedExpressionCompiler.Process(lambdaExpression);
        }

        // Evaluates an expression (not a LambdaExpression), e.g. 2 + 2.
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
            Expression<Func<object, object>> lambdaExpr = Expression.Lambda<Func<object, object>>(Expression.Convert(arg, typeof(object)), _unusedParameterExpr);
            return HangFire.ExpressionUtil.CachedExpressionCompiler.Process(lambdaExpr);
        }
    }
}
