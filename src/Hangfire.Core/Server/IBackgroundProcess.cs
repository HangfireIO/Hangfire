// This file is part of Hangfire. Copyright © 2013-2014 Sergey Odinokov.
// 
// Permission to use, copy, modify, and/or distribute this software for any
// purpose with or without fee is hereby granted.
// 
// THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES WITH
// REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY
// AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY SPECIAL, DIRECT,
// INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM
// LOSS OF USE, DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR
// OTHER TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR
// PERFORMANCE OF THIS SOFTWARE.

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