// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

#pragma warning disable 659 // overrides AddToHashCodeCombiner instead

namespace Hangfire.Common.ExpressionUtil
{
    // ConditionalExpression fingerprint class
    // Expression of form (test) ? ifTrue : ifFalse

    [SuppressMessage("Microsoft.Usage", "CA2218:OverrideGetHashCodeOnOverridingEquals", Justification = "Overrides AddToHashCodeCombiner() instead.")]
    [ExcludeFromCodeCoverage]
    internal sealed class ConditionalExpressionFingerprint : ExpressionFingerprint
    {
        public ConditionalExpressionFingerprint(ExpressionType nodeType, Type type)
            : base(nodeType, type)
        {
            // There are no properties on ConditionalExpression that are worth including in
            // the fingerprint.
        }

        public override bool Equals(object obj)
        {
            ConditionalExpressionFingerprint other = obj as ConditionalExpressionFingerprint;
            return (other != null)
                   && this.Equals(other);
        }
    }
}
