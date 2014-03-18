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
using HangFire.Common.Filters;
using HangFire.Common.States;
using HangFire.States;

namespace HangFire.Filters
{
    public class RetryAttribute : JobFilterAttribute, IStateChangingFilter
    {
        private int _attempts;
        private const int DefaultRetryAttempts = 3;

        public RetryAttribute()
        {
            Attempts = DefaultRetryAttempts;
        }

        public int Attempts
        {
            get { return _attempts; }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("value", "Attempts value must be equal or greater that zero.");
                }
                _attempts = value;
            }
        }

        public void OnStateChanging(StateChangingContext context)
        {
            if (context.CandidateState.Name != FailedState.StateName)
            {
                // This filter accepts only failed job state.
                return;
            }

            var retryCount = context.GetJobParameter<int>("RetryCount");
            
            if (retryCount < Attempts)
            {
                var delay = DateTime.UtcNow.AddSeconds(SecondsToDelay(retryCount));

                context.SetJobParameter("RetryCount", retryCount + 1);

                // If attempt number is less than max attempts, we should
                // schedule the job to run again later.
                context.CandidateState = new ScheduledState(delay)
                {
                    Reason = String.Format("Retry attempt {0} of {1}", retryCount + 1, Attempts)
                };
            }
        }

        // delayed_job uses the same basic formula
        private static int SecondsToDelay(long retryCount)
        {
            var random = new Random();
            return (int)Math.Round(
                Math.Pow(retryCount, 4) + 15 + (random.Next(30) * (retryCount + 1)));
        }
    }
}
