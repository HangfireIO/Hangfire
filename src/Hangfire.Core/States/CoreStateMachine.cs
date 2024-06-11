// This file is part of Hangfire. Copyright © 2013-2014 Hangfire OÜ.
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
using System.Collections.Generic;
using Hangfire.Annotations;

namespace Hangfire.States
{
    internal class CoreStateMachine : IStateMachine
    {
        private readonly Func<JobStorage, string, IEnumerable<IStateHandler>> _stateHandlersThunk;

        public CoreStateMachine()
            : this(GetStateHandlers)
        {
        }

        internal CoreStateMachine([NotNull] Func<JobStorage, string, IEnumerable<IStateHandler>> stateHandlersThunk)
        {
            if (stateHandlersThunk == null) throw new ArgumentNullException(nameof(stateHandlersThunk));
            _stateHandlersThunk = stateHandlersThunk;
        }

        public IState ApplyState(ApplyStateContext context)
        {
            foreach (var handler in _stateHandlersThunk(context.Storage, context.OldStateName))
            {
                handler.Unapply(context, context.Transaction);
            }

            context.Transaction.SetJobState(context.BackgroundJob.Id, context.NewState);

            foreach (var handler in _stateHandlersThunk(context.Storage, context.NewState.Name))
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

        private static IEnumerable<IStateHandler> GetStateHandlers(JobStorage storage, string stateName)
        {
            foreach (var globalHandler in GlobalStateHandlers.Handlers)
            {
                if (globalHandler.StateName.Equals(stateName, StringComparison.OrdinalIgnoreCase))
                {
                    yield return globalHandler;
                }
            }

            foreach (var storageHandler in storage.GetStateHandlers())
            {
                if (storageHandler.StateName.Equals(stateName, StringComparison.OrdinalIgnoreCase))
                {
                    yield return storageHandler;
                }
            }
        }
    }
}