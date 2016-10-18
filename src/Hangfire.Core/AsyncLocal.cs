// This file is part of Hangfire.
// Copyright © 2015 Sergey Odinokov.
// 
// Hangfire is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as 
// published by the Free Software Foundation, either version 3 
// of the License, or any later version.
// 
// Hangfire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public 
// License along with Hangfire. If not, see <http://www.gnu.org/licenses/>.

#if !NETSTANDARD1_3

using System;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Messaging;

namespace System.Threading
{
    /// <summary>
    /// A drop-in replacement for <see cref="System.Threading.AsyncLocal{T}"/> for pre-net46 environment.
    /// </summary>
    /// <typeparam name="T">Value type</typeparam>
    internal class AsyncLocal<T>
    {
        private readonly string _slotName;

        public AsyncLocal()
        {
            _slotName = Guid.NewGuid().ToString();
        }

        public T Value
        {
            get
            {
                var handle = CallContext.LogicalGetData(_slotName) as ObjectHandle;
                if (handle == null)
                {
                    return default(T);
                }

                // won't fail for value types, since nulls are never wrapped in ObjectHandle
                return (T)handle.Unwrap();
            }
            set
            {
                if (value == null)
                {
                    CallContext.LogicalSetData(_slotName, null);
                }
                else
                {
                    // wrap non-null values in ObjectHandle for cross-AppDomain safety
                    CallContext.LogicalSetData(_slotName, new ObjectHandle(value));
                }
            }
        }
    }
}

#endif