namespace HangFire.Tests
{
    [Queue(" $InvalidQueue")]
    public class InvalidQueueJob : BackgroundJob
    {
        public override void Perform()
        {
        }
    }
}
