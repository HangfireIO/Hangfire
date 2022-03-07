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

using System.Threading;

namespace Hangfire
{
    public class JobCancellationToken : IJobCancellationToken
    {
        private readonly bool _canceled;

        public JobCancellationToken(bool canceled)
        {
            _canceled = canceled;
            ShutdownToken = new CancellationToken(canceled);
        }

        public CancellationToken ShutdownToken { get; }

        public static IJobCancellationToken Null => null;

        public void ThrowIfCancellationRequested()
        {
            ShutdownToken.ThrowIfCancellationRequested();
        }
    }
}