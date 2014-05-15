// This file is part of HangFire.
// Copyright © 2013-2014 Sergey Odinokov.
// 
// HangFire is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as 
// published by the Free Software Foundation, either version 3 
// of the License, or any later version.
// 
// HangFire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public 
// License along with HangFire. If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using HangFire.Common;

namespace HangFire.States
{
    public class ElectStateContext : StateContext
    {
        private IState _candidateState;

        internal ElectStateContext(
            StateContext context, 
            IState candidateState, 
            string currentState)
            : base(context)
        {
            if (candidateState == null) throw new ArgumentNullException("candidateState");

            CandidateState = candidateState;
            CurrentState = currentState;
        }

        public IState CandidateState
        {
            get { return _candidateState; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value", "The CandidateState property can not be set to null.");
                }
                _candidateState = value;
            }
        }

        public string CurrentState { get; private set; }

        public void SetJobParameter<T>(string name, T value)
        {
            Connection.SetJobParameter(JobId, name, JobHelper.ToJson(value));
        }

        public T GetJobParameter<T>(string name)
        {
            return JobHelper.FromJson<T>(Connection.GetJobParameter(
                JobId, name));
        }

        internal IState ElectState(IEnumerable<IElectStateFilter> filters)
        {
            if (filters == null) throw new ArgumentNullException("filters");

            var statesToAppend = new List<IState>();

            foreach (var filter in filters)
            {
                var oldState = CandidateState;
                filter.OnStateElection(this);

                if (oldState != CandidateState)
                {
                    statesToAppend.Add(oldState);
                }
            }

            if (statesToAppend.Count > 0)
            {
                using (var transaction = Connection.CreateWriteTransaction())
                {
                    foreach (var state in statesToAppend)
                    {
                        transaction.AddJobState(JobId, state);
                    }

                    transaction.Commit();
                }
            }

            return CandidateState;
        }
    }
}