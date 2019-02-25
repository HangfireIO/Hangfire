// This file is part of Hangfire.
// Copyright © 2019 Sergey Odinokov.
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
using Hangfire.Annotations;

namespace Hangfire.Processing
{
    internal class AwaitableContext
    {
        private readonly MethodInfo _getAwaiter;
        private readonly PropertyInfo _isCompleted;
        private readonly MethodInfo _getResult;

        public AwaitableContext(
            [NotNull] MethodInfo getAwaiter,
            [NotNull] PropertyInfo isCompleted,
            [NotNull] MethodInfo getResult)
        {
            _getAwaiter = getAwaiter ?? throw new ArgumentNullException(nameof(getAwaiter));
            _isCompleted = isCompleted ?? throw new ArgumentNullException(nameof(isCompleted));
            _getResult = getResult ?? throw new ArgumentNullException(nameof(getResult));
        }

        public object GetAwaiter(object instance)
        {
            return _getAwaiter.Invoke(instance, null);
        }

        public bool IsCompleted(object awaiter)
        {
            return (bool)_isCompleted.GetValue(awaiter);
        }

        public object GetResult(object awaiter)
        {
            return _getResult.Invoke(awaiter, null);
        }
    }
}