using HangFire.Server;

namespace HangFire.Filters
{
    /// <summary>
    /// Provides the context for the <see cref="IServerFilter.OnPerforming"/>
    /// method of the <see cref="IServerFilter"/> interface.
    /// </summary>
    public class PerformingContext : PerformContext
    {
        internal PerformingContext(
            PerformContext context)
            : base(context)
        {
        }

        /// <summary>
        /// Gets or sets a value that indicates that this <see cref="PerformingContext"/>
        /// object was canceled.
        /// </summary>
        public bool Canceled { get; set; }
    }
}