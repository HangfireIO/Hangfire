using System;
using TechTalk.SpecFlow;

namespace HangFire.Tests
{
    [Binding]
    public class Transforms
    {
        [StepArgumentTransformation(@"in (\d+) days?")]
        public DateTime InXDaysTransform(int days)
        {
            return DateTime.Today.AddDays(days);
        }

        [StepArgumentTransformation(@"a (\w+) ago")]
        public DateTime ATimeAgo(string timeAgo)
        {
            var now = DateTime.UtcNow;
            if ("millisecond".Equals(timeAgo))
            {
                return now.AddMilliseconds(-1);
            }
            if ("second".Equals(timeAgo))
            {
                return now.AddSeconds(-1);
            }
            if ("minute".Equals(timeAgo))
            {
                return now.AddMinutes(-1);
            }
            if ("hour".Equals(timeAgo))
            {
                return now.AddHours(-1);
            }
            if ("day".Equals(timeAgo))
            {
                return now.AddDays(-1);
            }
            if ("month".Equals(timeAgo))
            {
                return now.AddMonths(-1);
            }

            throw new InvalidOperationException(String.Format("Wrong time unit '{0}'", timeAgo));
        }
    }
}
