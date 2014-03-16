// This file is part of HangFire.
// Copyright © 2013-2014 Sergey Odinokov.
// 
// HangFire is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// HangFire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with HangFire.  If not, see <http://www.gnu.org/licenses/>.

using System;
using HangFire.Storage;

namespace HangFire.Common.States
{
    public class StateChangingContext : StateContext
    {
        private State _candidateState;

        internal StateChangingContext(
            StateContext context, 
            State candidateState, 
            string currentState, 
            IStorageConnection connection)
            : base(context)
        {
            if (candidateState == null) throw new ArgumentNullException("candidateState");
            if (connection == null) throw new ArgumentNullException("connection");

            CandidateState = candidateState;
            CurrentState = currentState;
            Connection = connection;
        }

        public State CandidateState
        {
            get { return _candidateState; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value", "Candidate state can not be set to null.");
                }
                _candidateState = value;
            }
        }

        public string CurrentState { get; private set; }

        public IStorageConnection Connection { get; private set; }

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