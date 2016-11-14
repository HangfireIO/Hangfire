using System;
using System.Collections.Generic;
using System.Reflection;

namespace Hangfire
{
	public class RecurringJobBuilder : IRecurringJobBuilder
	{
		public IRecurringJobRegistry Registry { get; private set; }
		public RecurringJobBuilder(IRecurringJobRegistry registry)
		{
			Registry = registry;
		}

		public void Build(Func<IEnumerable<Type>> typesProvider)
		{
			if (typesProvider == null) throw new ArgumentNullException(nameof(typesProvider));

			foreach (var type in typesProvider())
			{
				var typeInfo = type.GetTypeInfo();
#if NETFULL
				foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
#else
				foreach (var method in type.GetTypeInfo().DeclaredMethods)
#endif
				{
					if (!method.IsDefined(typeof(RecurringJobAttribute), false)) continue;

					var attribute = method.GetCustomAttribute<RecurringJobAttribute>(false);

					if (attribute == null || !attribute.Enabled) continue;

					Registry.Register(method, attribute.Cron, attribute.TimeZone, attribute.Queue);
				}
			}
		}
	}
}
