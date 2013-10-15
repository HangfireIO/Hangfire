using HangFire.Client;

namespace HangFire.Filters
{
    public class CreatingContext : CreateContext
    {
        internal CreatingContext(CreateContext context)
            : base(context)
        {
        }

        public bool Canceled { get; set; }
    }
}