// This file is part of Hangfire.
// Copyright � 2013-2014 Sergey Odinokov.
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
using System.Threading;
using Hangfire.Client;

namespace Hangfire.States
{
    public interface IStateMachine : IJobCreator
    {
        IStateChangeProcess Process { get; }

        bool ChangeState(string jobId, IState toState, string[] fromStates, CancellationToken cancellationToken);
    }

    public static class StateMachineExtensions
    {
        public static bool ChangeState(this IStateMachine stateMachine,
            string jobId, IState toState, string[] fromStates)
        {
            using (var cts = new CancellationTokenSource())
            {
                cts.Cancel();
                return stateMachine.ChangeState(jobId, toState, fromStates, cts.Token);    
            }
        }
    }
}