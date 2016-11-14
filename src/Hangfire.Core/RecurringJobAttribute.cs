using System;
using Hangfire.States;

namespace Hangfire
{
	[AttributeUsage(AttributeTargets.Method)]
	public class RecurringJobAttribute : Attribute
	{
		public string Cron { get; private set; }
		public TimeZoneInfo TimeZone { get; private set; }
		public string Queue { get; private set; }
		public bool Enabled { get; set; } = true;
		public RecurringJobAttribute(string cron) : this(cron, TimeZoneInfo.Local) { }
		public RecurringJobAttribute(string cron, TimeZoneInfo timeZone) : this(cron, timeZone, EnqueuedState.DefaultQueue) { }
		public RecurringJobAttribute(string cron, TimeZoneInfo timeZone, string queue)
		{
			Cron = cron;
			TimeZone = timeZone;
			Queue = queue;
		}
	}
}
