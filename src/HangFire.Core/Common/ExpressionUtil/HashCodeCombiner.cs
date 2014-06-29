// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Hangfire.Common.ExpressionUtil
{
    // based on System.Web.Util.HashCodeCombiner
    [ExcludeFromCodeCoverage]
    internal class HashCodeCombiner
    {
        private long _combinedHash64 = 0x1505L;

        public int CombinedHash
        {
            get { return _combinedHash64.GetHashCode(); }
        }

        public void AddFingerprint(ExpressionFingerprint fingerprint)
        {
            if (fingerprint != null)
            {
                fingerprint.AddToHashCodeCombiner(this);
            }
            else
            {
                AddInt32(0);
            }
        }

        public void AddEnumerable(IEnumerable e)
        {
            if (e == null)
            {
                AddInt32(0);
            }
            else
            {
                int count = 0;
                foreach (object o in e)
                {
                    AddObject(o);
                    count++;
                }
                AddInt32(count);
            }
        }

        public void AddInt32(int i)
        {
            _combinedHash64 = ((_combinedHash64 << 5) + _combinedHash64) ^ i;
        }

        public void AddObject(object o)
        {
            int hashCode = (o != null) ? o.GetHashCode() : 0;
            AddInt32(hashCode);
        }
    }
}
