using System;
using System.Collections.Generic;
using HangFire.Storage;

namespace HangFire.States
{
    public interface IStateMachineFactory
    {
        IStateMachine Create(IStorageConnection connection);
    }

    public class StateMachineFactory : IStateMachineFactory
    {
        private readonly List<StateHandler> _handlers 
            = new List<StateHandler>();

        public StateMachineFactory(JobStorage storage)
        {
            if (storage == null) throw new ArgumentNullException("storage");

            _handlers.AddRange(GlobalStateHandlers.Handlers);
            _handlers.AddRange(storage.GetStateHandlers());
        }

        public IStateMachine Create(IStorageConnection connection)
        {
            if (connection == null) throw new ArgumentNullException("connection");
            
            return new StateMachine(connection, _handlers);
        }
    }
}
