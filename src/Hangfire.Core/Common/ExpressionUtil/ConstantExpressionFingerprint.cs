// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

#pragma warning disable 659 // overrides AddToHashCodeCombiner instead

namespace Hangfire.Common.ExpressionUtil
{
    // ConstantExpression fingerprint class
    //
    // A ConstantExpression might represent a captured local variable, so we can't compile
    // the value directly into the cached function. Instead, a placeholder is generated
    // and the value is hoisted into a local variables array. This placeholder can then
    // be compiled and cached, and the array lookup happens at runtime.

    [SuppressMessage("Microsoft.Usage", "CA2218:OverrideGetHashCodeOnOverridingEquals", Justification = "Overrides AddToHashCodeCombiner() instead.")]
    [ExcludeFromCodeCoverage]
    internal sealed class ConstantExpressionFingerprint : ExpressionFingerprint
    {
        public ConstantExpressionFingerprint(ExpressionType nodeType, Type type)
            : base(nodeType, type)
        {
            // There are no properties on ConstantExpression that are worth including in
            // the fingerprint.
        }

        public override bool Equals(object obj)
        {
            ConstantExpressionFingerprint other = obj as ConstantExpressionFingerprint;
            return (other != null)
                   && this.Equals(other);
        }
    }
}
