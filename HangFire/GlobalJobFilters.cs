using HangFire.Filters;

namespace HangFire
{
    /// <summary>
    /// Represents the global filter collection.
    /// </summary>
    public static class GlobalJobFilters
    {
        static GlobalJobFilters()
        {
            Filters = new GlobalJobFilterCollection();
            Filters.Add(new PreserveCultureAttribute());
            Filters.Add(new RetryAttribute { Attempts = 3 });
        }

        /// <summary>
        /// Gets the global filter collection.
        /// </summary>
        public static GlobalJobFilterCollection Filters { get; private set; }
    }
}
