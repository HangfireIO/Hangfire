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
    public class StatisticsHistoryFilterAttribute : JobFilterAttribute, IStateChangingFilter
    {
        public StatisticsHistoryFilterAttribute()
        {
            Order = 30;
        }

        public void OnStateChanging(StateChangingContext context)
        {
            using (var transaction = context.Connection.CreateWriteTransaction())
            {
                if (context.CandidateState.StateName == SucceededState.Name)
                {
                    var monthlySucceededKey = String.Format(
                        "stats:succeeded:{0}",
                        DateTime.UtcNow.ToString("yyyy-MM-dd"));

                    transaction.Values.Increment(monthlySucceededKey);
                    transaction.Values.ExpireIn(monthlySucceededKey, DateTime.UtcNow.AddMonths(1) - DateTime.UtcNow);

                    var hourlySucceededKey = String.Format(
                        "stats:succeeded:{0}",
                        DateTime.UtcNow.ToString("yyyy-MM-dd-HH"));

                    transaction.Values.Increment(hourlySucceededKey);
                    transaction.Values.ExpireIn(hourlySucceededKey, TimeSpan.FromDays(1));
                }
                else if (context.CandidateState.StateName == FailedState.Name)
                {
                    var monthlyFailedKey = String.Format(
                        "stats:failed:{0}", 
                        DateTime.UtcNow.ToString("yyyy-MM-dd"));

                    transaction.Values.Increment(monthlyFailedKey);
                    transaction.Values.ExpireIn(monthlyFailedKey, DateTime.UtcNow.AddMonths(1) - DateTime.UtcNow);

                    var hourlyFailedKey = String.Format(
                        "stats:failed:{0}",
                        DateTime.UtcNow.ToString("yyyy-MM-dd-HH"));

                    transaction.Values.Increment(hourlyFailedKey);
                    transaction.Values.ExpireIn(hourlyFailedKey, TimeSpan.FromDays(1));
                }

                transaction.Commit();
            }
        }
    }
}
