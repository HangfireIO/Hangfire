// This file is part of Hangfire. Copyright © 2013-2014 Hangfire OÜ.
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
using Hangfire.Annotations;

#pragma warning disable 618 // Obsolete member

namespace Hangfire.Server
{
    /// <summary>
    /// Provides methods for defining processes that will be executed in a
    /// background thread by <see cref="BackgroundProcessingServer"/>.
    /// </summary>
    /// 
    /// <remarks>
    /// Needs a wait.
    /// Cancellation token
    /// Connection disposal
    /// </remarks>
    /// 
    /// <seealso cref="BackgroundProcessingServer"/>
    public interface IBackgroundProcess : IServerProcess
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="context">Context for a background process.</param>
        /// <exception cref="ArgumentNullException"><paramref name="context"/> is null.</exception>
        void Execute([NotNull] BackgroundProcessContext context);
    }
}