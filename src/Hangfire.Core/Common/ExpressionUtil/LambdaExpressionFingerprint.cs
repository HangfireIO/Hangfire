// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

#pragma warning disable 659 // overrides AddToHashCodeCombiner instead

namespace Hangfire.Common.ExpressionUtil
{
    // LambdaExpression fingerprint class
    // Represents a lambda expression (root element in Expression<T>)

    [SuppressMessage("Microsoft.Usage", "CA2218:OverrideGetHashCodeOnOverridingEquals", Justification = "Overrides AddToHashCodeCombiner() instead.")]
    [ExcludeFromCodeCoverage]
    internal sealed class LambdaExpressionFingerprint : ExpressionFingerprint
    {
        public LambdaExpressionFingerprint(ExpressionType nodeType, Type type)
            : base(nodeType, type)
        {
            // There are no properties on LambdaExpression that are worth including in
            // the fingerprint.
        }

        public override bool Equals(object obj)
        {
            LambdaExpressionFingerprint other = obj as LambdaExpressionFingerprint;
            return (other != null)
                   && this.Equals(other);
        }
    }
}
