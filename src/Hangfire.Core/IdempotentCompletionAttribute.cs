// This file is part of Hangfire. Copyright © 2019 Sergey Odinokov.
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
using Hangfire.Common;
using Hangfire.States;

namespace Hangfire
{
    public class IdempotentCompletionAttribute : JobFilterAttribute, IElectStateFilter
    {
        public IdempotentCompletionAttribute()
        {
            Order = 0;
        }

        public void OnStateElection(ElectStateContext context)
        {
            if (String.IsNullOrEmpty(context.CurrentState)) return;

            var serializedState = context.GetJobParameter<string>("Completion");

            if (!String.IsNullOrEmpty(serializedState))
            {
                if (context.CandidateState is ProcessingState || context.CandidateState.IsFinal)
                {
                    context.CandidateState = SerializationHelper.Deserialize<IState>(serializedState, SerializationOption.TypedInternal);
                }
            }
            else if (context.CandidateState.IsFinal)
            {
                context.SetJobParameter("Completion", SerializationHelper.Serialize(context.CandidateState, SerializationOption.TypedInternal));
            }
        }
    }
}