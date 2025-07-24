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

using System.Threading;

namespace Hangfire
{
    public interface IJobCancellationToken
    {
        CancellationToken ShutdownToken { get; }

        /// <summary>
        /// Throws a <see cref="System.OperationCanceledException">OperationCanceledException</see> if
        /// this token has had cancellation requested.
        /// </summary>
        /// <remarks>
        /// This method provides functionality equivalent to:
        /// <code>
        /// if (token.ShutdownToken.IsCancellationRequested)
        ///    throw new OperationCanceledException(token);
        /// </code>
        /// </remarks>
        /// <exception cref="System.OperationCanceledException">The token has had cancellation requested.</exception>
        void ThrowIfCancellationRequested();
    }
}
