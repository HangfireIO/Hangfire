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
    /// Provides a mechanism for performing custom actions when applying or
    /// unapplying the state of a background job by <see cref="StateMachine"/>.
    /// </summary>
    public interface IStateHandler
    {
        /// <summary>
        /// Gets the name of a state, for which custom actions will be
        /// performed.
        /// </summary>
        string StateName { get; }

        /// <summary>
        /// Performs additional actions when applying a state whose name is
        /// equal to the <see cref="StateName"/> property.
        /// </summary>
        /// <param name="context">The context of a state applying process.</param>
        /// <param name="transaction">The current transaction of a state applying process.</param>
        void Apply(ApplyStateContext context, IWriteOnlyTransaction transaction);

        /// <summary>
        /// Performs additional actions when unapplying a state whose name
        /// is equal to the <see cref="StateName"/> property.
        /// </summary>
        /// <param name="context">The context of a state applying process.</param>
        /// <param name="transaction">The current transaction of a state applying process.</param>
        void Unapply(ApplyStateContext context, IWriteOnlyTransaction transaction);
    }
}