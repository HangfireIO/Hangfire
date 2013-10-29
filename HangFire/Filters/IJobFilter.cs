namespace HangFire.Filters
{
    /// <summary>
    /// Defines members that specify the order of filters and 
    /// whether multiple filters are allowed.
    /// </summary>
    public interface IJobFilter
    {
        /// <summary>
        /// When implemented in a class, gets or sets a value 
        /// that indicates whether multiple filters are allowed.
        /// </summary>
        bool AllowMultiple { get; }

        /// <summary>
        /// When implemented in a class, gets the filter order.
        /// </summary>
        int Order { get; }
    }
}