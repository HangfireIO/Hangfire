// This file is part of Hangfire. Copyright © 2022 Hangfire OÜ.
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

using System;
using System.Reflection;

namespace Hangfire.SqlServer
{
    internal static class ExceptionTypeHelper
    {
        private static readonly Type OutOfMemoryType = typeof(OutOfMemoryException);
#if !NETSTANDARD1_3
        private static readonly Type StackOverflowType = typeof(StackOverflowException);
        private static readonly Type ThreadAbortType = typeof(System.Threading.ThreadAbortException);
        private static readonly Type AccessViolationType = typeof(AccessViolationException);
#endif
        private static readonly Type SecurityType = typeof(System.Security.SecurityException);
 
        internal static bool IsCatchableExceptionType(this Exception e)
        {
            var type = e.GetType();
            return type != OutOfMemoryType &&
#if !NETSTANDARD1_3
                   type != StackOverflowType &&
                   type != ThreadAbortType &&
                   type != AccessViolationType &&
#endif
                   !SecurityType.GetTypeInfo().IsAssignableFrom(type.GetTypeInfo());
        }
    }
}