namespace HangFire.Filters
{
    public class PerformingContext : PerformContext
    {
        internal PerformingContext(
            PerformContext context)
            : base(context)
        {
        }

        public bool Canceled { get; set; }
    }
}