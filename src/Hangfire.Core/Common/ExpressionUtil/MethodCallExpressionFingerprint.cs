// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

#pragma warning disable 659 // overrides AddToHashCodeCombiner instead

namespace Hangfire.Common.ExpressionUtil
{
    // MethodCallExpression fingerprint class
    // Expression of form xxx.Foo(...), xxx[...] (get_Item()), etc.

    [SuppressMessage("Microsoft.Usage", "CA2218:OverrideGetHashCodeOnOverridingEquals", Justification = "Overrides AddToHashCodeCombiner() instead.")]
    [ExcludeFromCodeCoverage]
    internal sealed class MethodCallExpressionFingerprint : ExpressionFingerprint
    {
        public MethodCallExpressionFingerprint(ExpressionType nodeType, Type type, MethodInfo method)
            : base(nodeType, type)
        {
            // Other properties on MethodCallExpression (like the argument count) are simply derived
            // from Type and Indexer, so they're not necessary for inclusion in the fingerprint.

            Method = method;
        }

        // http://msdn.microsoft.com/en-us/library/system.linq.expressions.methodcallexpression.method.aspx
        public MethodInfo Method { get; private set; }

        public override bool Equals(object obj)
        {
            MethodCallExpressionFingerprint other = obj as MethodCallExpressionFingerprint;
            return (other != null)
                   && Equals(this.Method, other.Method)
                   && this.Equals(other);
        }

        internal override void AddToHashCodeCombiner(HashCodeCombiner combiner)
        {
            combiner.AddObject(Method);
            base.AddToHashCodeCombiner(combiner);
        }
    }
}
