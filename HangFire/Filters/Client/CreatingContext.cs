using HangFire.Client;

namespace HangFire.Filters
{
    /// <summary>
    /// Provides the context for the <see cref="IClientFilter.OnCreating"/>
    /// method of the <see cref="IClientFilter"/> interface.
    /// </summary>
    public class CreatingContext : CreateContext
    {
        internal CreatingContext(CreateContext context)
            : base(context)
        {
        }

        /// <summary>
        /// Gets or sets a value that indicates that this <see cref="CreatingContext"/>
        /// object was canceled.
        /// </summary>
        public bool Canceled { get; set; }
    }
}