namespace HangFire.Filters
{
    /// <summary>
    /// Provides a registration point for filters.
    /// </summary>
    public static class JobFilterProviders
    {
        static JobFilterProviders()
        {
            Providers = new JobFilterProviderCollection();
            Providers.Add(GlobalJobFilters.Filters);
            Providers.Add(new JobFilterAttributeFilterProvider());
        }

        /// <summary>
        /// Provides a registration point for filters.
        /// </summary>
        public static JobFilterProviderCollection Providers { get; private set; }
    }
}
