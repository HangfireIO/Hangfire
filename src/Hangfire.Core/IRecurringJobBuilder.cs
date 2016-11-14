using System;
using System.Collections.Generic;

namespace Hangfire
{
	/// <summary>
	/// Build <see cref="RecurringJob"/> automatically.
	/// </summary>
	public interface IRecurringJobBuilder
	{
		/// <summary>
		/// Create <see cref="RecurringJob"/> within specified interface or class.
		/// </summary>
		/// <param name="typesProvider">Specified interface or class</param>
		void Build(Func<IEnumerable<Type>> typesProvider);
	}
}
