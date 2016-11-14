using System;
using System.Reflection;

namespace Hangfire
{
	public interface IRecurringJobRegistry
	{
		/// <summary>
		/// Register RecurringJob dynamically.
		/// </summary>
		/// <param name="method"></param>
		/// <param name="cron"></param>
		/// <param name="timeZone"></param>
		/// <param name="queue"></param>
		void Register(MethodInfo method, string cron, TimeZoneInfo timeZone, string queue);
	}
}
