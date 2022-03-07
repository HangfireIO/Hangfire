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

using Hangfire.Storage;

namespace Hangfire.States
{
    /// <summary>
    /// Provides methods that are required for a state changed filter.
    /// </summary>
    public interface IApplyStateFilter
    {
        /// <summary>
        /// Called after the specified state was applied
        /// to the job within the given transaction.
        /// </summary>
        void OnStateApplied(
            ApplyStateContext context, IWriteOnlyTransaction transaction);

        /// <summary>
        /// Called when the state with specified state was 
        /// unapplied from the job within the given transaction.
        /// </summary>
        void OnStateUnapplied(
            ApplyStateContext context, IWriteOnlyTransaction transaction);
    }
}