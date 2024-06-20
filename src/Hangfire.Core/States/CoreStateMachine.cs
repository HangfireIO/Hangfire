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
        private readonly Func<JobStorage, string, StateHandlersCollection> _stateHandlersThunk;

        public CoreStateMachine()
            : this(GetStateHandlers)
        {
        }

        internal CoreStateMachine([NotNull] Func<JobStorage, string, StateHandlersCollection> stateHandlersThunk)
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

        private static StateHandlersCollection GetStateHandlers(JobStorage storage, string stateName)
        {
            return new StateHandlersCollection(GlobalStateHandlers.Handlers, storage.GetStateHandlers(), stateName);
        }

        internal readonly struct StateHandlersCollection(
            IEnumerable<IStateHandler> globalHandlers,
            IEnumerable<IStateHandler> storageHandlers,
            string stateName)
        {
            public Enumerator GetEnumerator() => new Enumerator(globalHandlers, storageHandlers, stateName);

            public struct Enumerator
            {
                private readonly IEnumerator<IStateHandler> _globalEnumerator;
                private readonly IEnumerator<IStateHandler> _storageEnumerator;
                private readonly string _stateName;
                private IStateHandler _current;

                public Enumerator(IEnumerable<IStateHandler> globalHandlers, IEnumerable<IStateHandler> storageHandlers, string stateName)
                {
                    _globalEnumerator = globalHandlers.GetEnumerator();
                    _storageEnumerator = storageHandlers.GetEnumerator();
                    _stateName = stateName;
                    _current = default;
                }

                public bool MoveNext()
                {
                    while (_globalEnumerator.MoveNext())
                    {
                        var current = _globalEnumerator.Current!;
                        if (current.StateName.Equals(_stateName, StringComparison.OrdinalIgnoreCase))
                        {
                            _current = current;
                            return true;
                        }
                    }

                    while (_storageEnumerator.MoveNext())
                    {
                        var current = _storageEnumerator.Current!;
                        if (current.StateName.Equals(_stateName, StringComparison.OrdinalIgnoreCase))
                        {
                            _current = current;
                            return true;
                        }
                    }

                    _current = default;
                    return false;
                }

                public IStateHandler Current => _current;
            }
        }
    }
}