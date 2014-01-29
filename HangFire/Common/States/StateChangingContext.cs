// This file is part of HangFire.
// Copyright © 2013 Sergey Odinokov.
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
using ServiceStack.Redis;

namespace HangFire.Common.States
{
    public class StateChangingContext : StateContext
    {
        private JobState _candidateState;

        internal StateChangingContext(
            StateContext context, JobState candidateState, string currentState, IRedisClient redis)
            : base(context)
        {
            if (candidateState == null) throw new ArgumentNullException("candidateState");
            if (redis == null) throw new ArgumentNullException("redis");

            CandidateState = candidateState;
            CurrentState = currentState;
            Redis = redis;
        }

        public JobState CandidateState
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

        public IRedisClient Redis { get; private set; }

        public void SetJobParameter(string name, object value)
        {
            Redis.SetEntryInHash(
                String.Format("hangfire:job:{0}", JobId),
                name,
                JobHelper.ToJson(value));
        }

        public T GetJobParameter<T>(string name)
        {
            var value = Redis.GetValueFromHash(
                String.Format("hangfire:job:{0}", JobId),
                name);

            return JobHelper.FromJson<T>(value);
        }
    }
}