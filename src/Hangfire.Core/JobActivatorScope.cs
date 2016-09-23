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

using System;
using System.Threading;

namespace Hangfire
{
    /// <summary>
    /// Base class for <see cref="JobActivator"/> scopes.
    /// </summary>
    public abstract class JobActivatorScope : IDisposable
    {
        // ReSharper disable once InconsistentNaming
        private static readonly AsyncLocal<JobActivatorScope> _current
            = new AsyncLocal<JobActivatorScope>();

        private readonly JobActivatorScope _parent;
        private bool _disposed;

        protected JobActivatorScope()
        {
            _parent = _current.Value;
            _current.Value = this;
            _disposed = false;
        }

        /// <summary>
        /// Returns <see cref="JobActivatorScope"/> the code is running in.
        /// </summary>
        public static JobActivatorScope Current => _current.Value;

        /// <summary>
        /// Returns an enclosing <see cref="JobActivatorScope"/> for this scope.
        /// </summary>
        internal JobActivatorScope ParentScope => _parent;

        /// <summary>
        /// Resolve and activate an instance of <paramref name="type"/>.
        /// </summary>
        /// <param name="type">Class or interface type to activate</param>
        /// <returns>
        /// Instance of a <paramref name="type"/>, 
        /// or <c>null</c> if the specified type cannot be activated
        /// </returns>
        public abstract object Resolve(Type type);

        /// <summary>
        /// Called when current <see cref="JobActivatorScope"/> is about to be disposed.
        /// Subclasses may override this to perform some cleanup before leaving scope.
        /// </summary>
        protected virtual void DisposeScope()
        {
        }

        /// <summary>
        /// Throws <seealso cref="ObjectDisposedException"/> if the current scope is already disposed.
        /// First thing for subclasses to check in their <seealso cref="Resolve(Type)"/> implementations!
        /// </summary>
        protected void AssertNotDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(JobActivatorScope));
        }

        public void Dispose()
        {
            if (_disposed) return;

            // immediately mark as disposed, so Dispose() will never be reentered, 
            // even if mistakenly called from within DisposeScope() override
            _disposed = true;

            if (_current.Value != this)
                throw new InvalidOperationException("Messed up JobActivatorScope dispose order");

            try
            {
                DisposeScope();
            }
            finally
            {
                // restore previous scope
                _current.Value = _parent;
            }
        }
    }
}