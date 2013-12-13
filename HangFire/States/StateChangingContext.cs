using System;
using ServiceStack.Redis;

namespace HangFire.States
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
    }
}