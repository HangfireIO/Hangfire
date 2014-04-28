using System;
using System.Diagnostics;
using Xunit;

namespace HangFire.Core.Tests
{
    internal class PossibleHangingFactAttribute : FactAttribute
    {
        public PossibleHangingFactAttribute()
        {
            Timeout = Debugger.IsAttached ? Int32.MaxValue : 30 * 1000;
        }
    }
}
