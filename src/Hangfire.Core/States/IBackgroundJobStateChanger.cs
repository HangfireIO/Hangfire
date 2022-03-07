// This file is part of Hangfire. Copyright � 2013-2014 Sergey Odinokov.
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

namespace Hangfire.States
{
    public interface IBackgroundJobStateChanger
    {
        /// <summary>
        /// Attempts to change the state of a job, respecting any applicable job filters and state handlers.
        /// </summary>
        /// <returns><c>Null</c> if a constraint has failed, otherwise the final applied state</returns>
        /// <remarks>Also ensures that the job data can be loaded for this job</remarks>
        IState ChangeState(StateChangeContext context);
    }
}