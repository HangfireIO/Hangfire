// This file is part of Hangfire. Copyright © 2013-2014 Sergey Odinokov.
// 
// Permission to use, copy, modify, and/or distribute this software for any
// purpose with or without fee is hereby granted.
// 
// THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES WITH
// REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY
// AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY SPECIAL, DIRECT,
// INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM
// LOSS OF USE, DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR
// OTHER TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR
// PERFORMANCE OF THIS SOFTWARE.

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
