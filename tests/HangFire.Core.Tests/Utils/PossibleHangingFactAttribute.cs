using Xunit;

namespace HangFire.Core.Tests
{
    internal class PossibleHangingFactAttribute : FactAttribute
    {
        public PossibleHangingFactAttribute()
        {
            Timeout = 30 * 1000;
        }
    }
}
