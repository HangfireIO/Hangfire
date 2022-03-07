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

using System;
using Hangfire.Annotations;

namespace Hangfire.States
{
    internal class CoreStateMachine : IStateMachine
    {
        private readonly Func<JobStorage, StateHandlerCollection> _stateHandlersThunk;

        public CoreStateMachine()
            : this(GetStateHandlers)
        {
        }

        internal CoreStateMachine([NotNull] Func<JobStorage, StateHandlerCollection> stateHandlersThunk)
        {
            if (stateHandlersThunk == null) throw new ArgumentNullException(nameof(stateHandlersThunk));
            _stateHandlersThunk = stateHandlersThunk;
        }

        public IState ApplyState(ApplyStateContext context)
        {
            var handlers = _stateHandlersThunk(context.Storage);

            foreach (var handler in handlers.GetHandlers(context.OldStateName))
            {
                handler.Unapply(context, context.Transaction);
            }

            context.Transaction.SetJobState(context.BackgroundJob.Id, context.NewState);

            foreach (var handler in handlers.GetHandlers(context.NewState.Name))
            {
                handler.Apply(context, context.Transaction);
            }

            if (context.NewState.IsFinal)
            {
                context.Transaction.ExpireJob(context.BackgroundJob.Id, context.JobExpirationTimeout);
            }
            else
            {
                context.Transaction.PersistJob(context.BackgroundJob.Id);
            }

            return context.NewState;
        }

        private static StateHandlerCollection GetStateHandlers(JobStorage storage)
        {
            var stateHandlers = new StateHandlerCollection();
            stateHandlers.AddRange(GlobalStateHandlers.Handlers);
            stateHandlers.AddRange(storage.GetStateHandlers());

            return stateHandlers;
        }
    }
}