using System;
using Hangfire.States;

namespace Hangfire
{
	/// <summary>
	/// Creating <see cref="RecurringJob"/> with interface/instance/static method automatically.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method)]
	public class RecurringJobAttribute : Attribute
	{
		/// <summary>
		/// Cron expressions
		/// </summary>
		public string Cron { get; private set; }
		/// <summary>
		/// TimeZoneInfo
		/// </summary>
		public TimeZoneInfo TimeZone { get; private set; }
		/// <summary>
		/// Queue name
		/// </summary>
		public string Queue { get; private set; }
		/// <summary>
		/// Whether to build RecurringJob automatically, default value is true. 
		/// </summary>
		public bool Enabled { get; set; } = true;
		/// <summary>
		/// Initializes a new instance of the <see cref="RecurringJobAttribute"/>
		/// </summary>
		/// <param name="cron">Cron expressions</param>
		public RecurringJobAttribute(string cron) : this(cron, TimeZoneInfo.Local) { }
		/// <summary>
		/// Initializes a new instance of the <see cref="RecurringJobAttribute"/>
		/// </summary>
		/// <param name="cron">Cron expressions</param>
		/// <param name="timeZone"><see cref="TimeZoneInfo"/></param>
		public RecurringJobAttribute(string cron, TimeZoneInfo timeZone) : this(cron, timeZone, EnqueuedState.DefaultQueue) { }
		/// <summary>
		/// Initializes a new instance of the <see cref="RecurringJobAttribute"/>
		/// </summary>
		/// <param name="cron">Cron expressions</param>
		/// <param name="timeZone"><see cref="TimeZoneInfo"/></param>
		/// <param name="queue">Queue name</param>
		public RecurringJobAttribute(string cron, TimeZoneInfo timeZone, string queue)
		{
			Cron = cron;
			TimeZone = timeZone;
			Queue = queue;
		}
	}
}
