// This file is part of Hangfire. Copyright © 2016 Sergey Odinokov.
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
using System.Threading.Tasks;
using Hangfire.Annotations;

// ReSharper disable once CheckNamespace
namespace Hangfire.Dashboard
{
    [Obsolete("Use IDashboardDispatcher-based dispatchers instead. Will be removed in 2.0.0.")]
    public class RequestDispatcherWrapper : IDashboardDispatcher
    {
        private readonly IRequestDispatcher _dispatcher;
        
        public RequestDispatcherWrapper([NotNull] IRequestDispatcher dispatcher)
        {
            if (dispatcher == null) throw new ArgumentNullException(nameof(dispatcher));
            _dispatcher = dispatcher;
        }

        public Task Dispatch(DashboardContext context)
        {
            return _dispatcher.Dispatch(RequestDispatcherContext.FromDashboardContext(context));
        }
    }
}