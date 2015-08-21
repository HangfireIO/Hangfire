// This file is part of Hangfire.
// Copyright © 2013-2014 Sergey Odinokov.
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
using System.Linq;
using Hangfire.Annotations;
using Hangfire.Common;
using Hangfire.States;

namespace Hangfire.Client
{
    public class StateMachineJobCreationProcess : IJobCreationProcess
    {
        private readonly IStateMachineFactoryFactory _stateMachineFactoryFactory;

        public StateMachineJobCreationProcess()
            : this(new StateMachineFactoryFactory())
        {
        }

        public StateMachineJobCreationProcess([NotNull] IStateMachineFactoryFactory stateMachineFactoryFactory)
        {
            if (stateMachineFactoryFactory == null) throw new ArgumentNullException("stateMachineFactoryFactory");
            _stateMachineFactoryFactory = stateMachineFactoryFactory;
        }

        public string Run(CreateContext context)
        {
            var stateMachineFactory = _stateMachineFactoryFactory.CreateFactory(context.Storage);
            var stateMachine = stateMachineFactory.Create(context.Connection);

            var parameters = context.Parameters.ToDictionary(x => x.Key, x => JobHelper.ToJson(x.Value));

            return stateMachine.CreateJob(context.Job, parameters, context.InitialState);
        }
    }
}