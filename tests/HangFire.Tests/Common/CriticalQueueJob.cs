namespace HangFire.Tests
{
    [Queue("critical")]
    public class CriticalQueueJob : BackgroundJob
    {
        public override void Perform()
        {
        }
    }
}
