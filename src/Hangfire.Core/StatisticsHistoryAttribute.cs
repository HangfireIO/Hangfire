// This file is part of Hangfire. Copyright © 2013-2014 Hangfire OÜ.
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
using System.Globalization;
using Hangfire.Common;
using Hangfire.States;

namespace Hangfire
{
    public sealed class StatisticsHistoryAttribute : JobFilterAttribute, IElectStateFilter
    {
        public StatisticsHistoryAttribute()
        {
            Order = 30;
        }

        public void OnStateElection(ElectStateContext context)
        {
            if (context.CandidateState.Name == SucceededState.StateName)
            {
                context.Transaction.IncrementCounter(
                    $"stats:succeeded:{DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}",
                    DateTime.UtcNow.AddMonths(1) - DateTime.UtcNow);

                context.Transaction.IncrementCounter(
                    $"stats:succeeded:{DateTime.UtcNow.ToString("yyyy-MM-dd-HH")}",
                    TimeSpan.FromDays(1));
            }
            else if (context.CandidateState.Name == FailedState.StateName)
            {
                context.Transaction.IncrementCounter(
                    $"stats:failed:{DateTime.UtcNow.ToString("yyyy-MM-dd")}",
                    DateTime.UtcNow.AddMonths(1) - DateTime.UtcNow);

                context.Transaction.IncrementCounter(
                    $"stats:failed:{DateTime.UtcNow.ToString("yyyy-MM-dd-HH")}",
                    TimeSpan.FromDays(1));
            }
        }
    }
}
