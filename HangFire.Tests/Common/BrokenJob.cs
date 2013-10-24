using System;

namespace HangFire.Tests
{
    internal class BrokenJob : BackgroundJob
    {
        public override void Perform()
        {
            throw new NotSupportedException("The job is broken!");
        }
    }
}
