// This file is part of Hangfire. Copyright © 2019 Hangfire OÜ.
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