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
        }

        /// <summary>
        /// Gets the global filter collection.
        /// </summary>
        public static GlobalJobFilterCollection Filters { get; private set; }
    }
}
