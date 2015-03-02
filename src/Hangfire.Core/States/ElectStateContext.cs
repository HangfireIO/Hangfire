﻿// This file is part of Hangfire.
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
using System.Collections.Generic;
using System.Linq;
using Hangfire.Annotations;
using Hangfire.Common;
using Hangfire.Storage;

namespace Hangfire.States
{
    public class ElectStateContext : StateContext
    {
        private readonly IList<IState> _traversedStates = new List<IState>();
        private IState _candidateState;

        public ElectStateContext(
            [NotNull] StateContext context, 
            [NotNull] IStorageConnection connection, 
            [NotNull] IStateMachine stateMachine,
            [NotNull] IState candidateState, 
            [CanBeNull] string currentState)
            : base(context)
        {
            if (connection == null) throw new ArgumentNullException("connection");
            if (stateMachine == null) throw new ArgumentNullException("stateMachine");
            if (candidateState == null) throw new ArgumentNullException("candidateState");

            _candidateState = candidateState;

            Connection = connection;
            StateMachine = stateMachine;
            CurrentState = currentState;
        }

        [NotNull]
        public IStorageConnection Connection { get; private set; }

        [NotNull]
        public IStateMachine StateMachine { get; private set; }

        [NotNull]
        public IState CandidateState
        {
            get { return _candidateState; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value", "The CandidateState property can not be set to null.");
                }

                if (_candidateState != value)
                {
                    _traversedStates.Add(_candidateState);
                    _candidateState = value;
                }
            }
        }

        [CanBeNull]
        public string CurrentState { get; private set; }

        [NotNull]
        public IState[] TraversedStates { get { return _traversedStates.ToArray(); } }

        public void SetJobParameter<T>(string name, T value)
        {
            Connection.SetJobParameter(JobId, name, JobHelper.ToJson(value));
        }

        public T GetJobParameter<T>(string name)
        {
            return JobHelper.FromJson<T>(Connection.GetJobParameter(
                JobId, name));
        }
    }
}