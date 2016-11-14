using System;
using System.Reflection;

namespace Hangfire
{
	/// <summary>
	/// Register <see cref="RecurringJob"/> dynamically.
	/// </summary>
	public interface IRecurringJobRegistry
	{
		/// <summary>
		/// Register RecurringJob via <see cref="MethodInfo"/>.
		/// </summary>
		/// <param name="method">the specified method marked by <see cref="RecurringJobAttribute"/> </param>
		/// <param name="cron">Cron expressions</param>
		/// <param name="timeZone"><see cref="TimeZoneInfo"/></param>
		/// <param name="queue">Queue name</param>
		void Register(MethodInfo method, string cron, TimeZoneInfo timeZone, string queue);
	}
}
