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
using HangFire.Common.Filters;
using HangFire.Common.States;
using HangFire.States;

namespace HangFire
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class StatisticsHistoryFilterAttribute : JobFilterAttribute, IElectStateFilter
    {
        public StatisticsHistoryFilterAttribute()
        {
            Order = 30;
        }

        public void OnStateElection(ElectStateContext context)
        {
            using (var transaction = context.Connection.CreateWriteTransaction())
            {
                if (context.CandidateState.Name == SucceededState.StateName)
                {
                    transaction.IncrementCounter(
                        String.Format(
                            "stats:succeeded:{0}",
                            DateTime.UtcNow.ToString("yyyy-MM-dd")),
                        DateTime.UtcNow.AddMonths(1) - DateTime.UtcNow);

                    transaction.IncrementCounter(
                        String.Format(
                            "stats:succeeded:{0}",
                            DateTime.UtcNow.ToString("yyyy-MM-dd-HH")),
                        TimeSpan.FromDays(1));
                }
                else if (context.CandidateState.Name == FailedState.StateName)
                {
                    transaction.IncrementCounter(
                        String.Format(
                            "stats:failed:{0}", 
                            DateTime.UtcNow.ToString("yyyy-MM-dd")),
                        DateTime.UtcNow.AddMonths(1) - DateTime.UtcNow);

                    transaction.IncrementCounter(
                        String.Format(
                            "stats:failed:{0}",
                            DateTime.UtcNow.ToString("yyyy-MM-dd-HH")),
                        TimeSpan.FromDays(1));
                }

                transaction.Commit();
            }
        }
    }
}
