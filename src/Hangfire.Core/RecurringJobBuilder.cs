using System;
using System.Collections.Generic;
using System.Reflection;

namespace Hangfire
{
    /// <summary>
    /// Build <see cref="RecurringJob"/> automatically, <see cref="IRecurringJobBuilder"/> interface.
    /// </summary>
    public class RecurringJobBuilder : IRecurringJobBuilder
    {
        /// <summary>
        /// <see cref="IRecurringJobRegistry"/> interface.
        /// </summary>
        public IRecurringJobRegistry Registry { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RecurringJobBuilder"/>	with <see cref="IRecurringJobRegistry"/>.
        /// </summary>
        /// <param name="registry"><see cref="IRecurringJobRegistry"/> interface.</param>
        public RecurringJobBuilder(IRecurringJobRegistry registry)
        {
            Registry = registry;
        }

        /// <summary>
        /// Create <see cref="RecurringJob"/> within specified interface or class.
        /// </summary>
        /// <param name="typesProvider">Specified interface or class</param>
        public void Build(Func<IEnumerable<Type>> typesProvider)
        {
            if (typesProvider == null) throw new ArgumentNullException(nameof(typesProvider));

            foreach (var type in typesProvider())
            {
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
