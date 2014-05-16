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
using Common.Logging;
using HangFire.Common;
using HangFire.States;

namespace HangFire
{
    public class RetryAttribute : JobFilterAttribute, IElectStateFilter
    {
        private static readonly ILog Logger = LogManager.GetCurrentClassLogger();

        private int _attempts;
        private const int DefaultRetryAttempts = 10;

        public RetryAttribute()
            : this(DefaultRetryAttempts)
        {
        }

        public RetryAttribute(int attempts)
        {
            Attempts = attempts;
            LogEvents = true;
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

        public bool LogEvents { get; set; }

        public void OnStateElection(ElectStateContext context)
        {
            var failedState = context.CandidateState as FailedState;
            if (failedState == null)
            {
                // This filter accepts only failed job state.
                return;
            }
            
            var retryCount = context.GetJobParameter<int>("RetryCount");
            
            if (retryCount < Attempts)
            {
                var delay = TimeSpan.FromSeconds(SecondsToDelay(retryCount));

                context.SetJobParameter("RetryCount", retryCount + 1);

                if (LogEvents)
                {
                    Logger.WarnFormat(
                        "", 
                        failedState.Exception, 
                        "");
                }

                // If attempt number is less than max attempts, we should
                // schedule the job to run again later.
                context.CandidateState = new ScheduledState(delay)
                {
                    Reason = String.Format("Retry attempt {0} of {1}", retryCount + 1, Attempts)
                };
            }
            else
            {
                if (LogEvents)
                {
                    Logger.ErrorFormat("", failedState.Exception, "");
                }
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
