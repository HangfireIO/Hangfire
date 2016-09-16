﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

#pragma warning disable 659 // overrides AddToHashCodeCombiner instead

namespace Hangfire.Common.ExpressionUtil
{
    // UnaryExpression fingerprint class
    // The most common appearance of a UnaryExpression is a cast or other conversion operator

    [SuppressMessage("Microsoft.Usage", "CA2218:OverrideGetHashCodeOnOverridingEquals", Justification = "Overrides AddToHashCodeCombiner() instead.")]
    [ExcludeFromCodeCoverage]
    internal sealed class UnaryExpressionFingerprint : ExpressionFingerprint
    {
        public UnaryExpressionFingerprint(ExpressionType nodeType, Type type, MethodInfo method)
            : base(nodeType, type)
        {
            // Other properties on UnaryExpression (like IsLifted / IsLiftedToNull) are simply derived
            // from Type and NodeType, so they're not necessary for inclusion in the fingerprint.

            Method = method;
        }

        // http://msdn.microsoft.com/en-us/library/system.linq.expressions.unaryexpression.method.aspx
        public MethodInfo Method { get; }

        public override bool Equals(object obj)
        {
            UnaryExpressionFingerprint other = obj as UnaryExpressionFingerprint;
            return (other != null)
                   && Equals(Method, other.Method)
                   && Equals(other);
        }

        internal override void AddToHashCodeCombiner(HashCodeCombiner combiner)
        {
            combiner.AddObject(Method);
            base.AddToHashCodeCombiner(combiner);
        }
    }
}
